using System.Globalization;
using System.Runtime.InteropServices;
using Graph3D.Geometry;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using NativeWindow = OpenTK.Windowing.Desktop.NativeWindow;

namespace Graph3D.Rendering;

/// <summary>
/// Renders on the GPU with OpenGL 3.3. Same approach and shaders as <see cref="Direct3DRenderer"/>,
/// written in GLSL. OpenGL needs a context, which comes with a window: a hidden one is created and the
/// picture is drawn off-screen (multisampled) and read back into the <see cref="FrameBuffer"/>.
/// OpenGL cannot choose the graphics card; Windows and the graphics driver decide.
/// </summary>
public sealed class OpenGLRenderer : IRenderer, IDisposable
{
    private readonly NativeWindow window;
    private readonly int sceneProgram;
    private readonly int backgroundProgram;
    private readonly int uniformBuffer;
    private readonly int emptyVertexArray;
    private readonly int sampleCount;

    private int multisampleFramebuffer, colorRenderbuffer, depthRenderbuffer;
    private int resolveFramebuffer, resolveRenderbuffer;
    private int width, height;

    private Mesh? uploadedMesh;
    private Color uploadedColor;
    private readonly int[] vertexArrays = new int[3];
    private readonly int[] vertexBuffers = new int[3];
    private readonly int[] vertexCounts = new int[3];

    private OpenGLRenderer(NativeWindow window)
    {
        this.window = window;

        string renderer = GL.GetString(StringName.Renderer) ?? "unknown GPU";
        int slash = renderer.IndexOf('/');
        GpuName = (slash > 0 ? renderer[..slash] : renderer).Trim();
        Version = GL.GetString(StringName.Version) ?? string.Empty;

        sceneProgram = LinkProgram(SceneVertexShader, SceneFragmentShader);
        backgroundProgram = LinkProgram(BackgroundVertexShader, BackgroundFragmentShader);

        uniformBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.UniformBuffer, uniformBuffer);
        GL.BufferData(BufferTarget.UniformBuffer, Marshal.SizeOf<FrameConstants>(), IntPtr.Zero, BufferUsageHint.DynamicDraw);
        foreach (int program in (ReadOnlySpan<int>)[sceneProgram, backgroundProgram])
            GL.UniformBlockBinding(program, GL.GetUniformBlockIndex(program, "Frame"), 0);

        emptyVertexArray = GL.GenVertexArray(); // the core profile needs one bound even without vertex data
        sampleCount = Math.Min(4, GL.GetInteger(GetPName.MaxSamples));
    }

    /// <summary>The graphics card the driver chose, as OpenGL reports it.</summary>
    public string GpuName { get; }

    /// <summary>OpenGL version string reported by the driver.</summary>
    public string Version { get; }

    public string Name => $"OpenGL · {GpuName}";

    /// <summary>Creates an OpenGL 3.3 context. Throws when the graphics driver does not provide one.</summary>
    public static OpenGLRenderer Create()
    {
        var settings = new NativeWindowSettings
        {
            Title = "Graph3D OpenGL",
            StartVisible = false,
            ClientSize = new OpenTK.Mathematics.Vector2i(16, 16),
            API = ContextAPI.OpenGL,
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core,
            Flags = ContextFlags.ForwardCompatible,
        };
        var window = new NativeWindow(settings);
        try
        {
            window.MakeCurrent();
            return new OpenGLRenderer(window);
        }
        catch
        {
            window.Dispose();
            throw;
        }
    }

    public void Render(Mesh mesh, Camera camera, FrameBuffer target, RenderSettings settings)
    {
        window.MakeCurrent();
        EnsureTargets(target.Width, target.Height);
        EnsureGeometry(mesh, settings.BaseColor);

        var frame = FrameConstants.Create(mesh, camera, width, height, settings);
        GL.BindBuffer(BufferTarget.UniformBuffer, uniformBuffer);
        GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, Marshal.SizeOf<FrameConstants>(), ref frame);
        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, uniformBuffer);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, multisampleFramebuffer);
        GL.Viewport(0, 0, width, height);
        GL.Enable(EnableCap.Multisample);
        GL.Disable(EnableCap.CullFace); // lit from both sides, like the software renderer
        GL.Disable(EnableCap.Blend);
        GL.ClearColor(0, 0, 0, 1);
        GL.ClearDepth(1);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Background gradient: one triangle that covers the screen.
        GL.Disable(EnableCap.DepthTest);
        GL.UseProgram(backgroundProgram);
        GL.BindVertexArray(emptyVertexArray);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        int slot = settings.Mode switch { RenderMode.Points => 2, RenderMode.Wireframe => 1, _ => 0 };
        if (vertexCounts[slot] > 0)
        {
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);
            if (settings.Mode == RenderMode.Points)
            {
                GL.Enable(EnableCap.Blend); // soft edges of the round dots
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            }
            GL.UseProgram(sceneProgram);
            GL.BindVertexArray(vertexArrays[slot]);
            GL.DrawArrays(settings.Mode == RenderMode.Wireframe ? PrimitiveType.Lines : PrimitiveType.Triangles, 0, vertexCounts[slot]);
        }

        // Resolve the multisampled picture and copy it into the frame buffer. The scene is drawn upside
        // down (see Project in the shader), so OpenGL's bottom-up rows come out top-down.
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, multisampleFramebuffer);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, resolveFramebuffer);
        GL.BlitFramebuffer(0, 0, width, height, 0, 0, width, height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, resolveFramebuffer);
        GL.PixelStore(PixelStoreParameter.PackAlignment, 4);
        GL.ReadPixels(0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, target.Pixels);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        var error = GL.GetError();
        if (error != ErrorCode.NoError)
            throw new InvalidOperationException($"OpenGL error: {error}.");
    }

    private void EnsureTargets(int newWidth, int newHeight)
    {
        if (multisampleFramebuffer != 0 && newWidth == width && newHeight == height)
            return;

        DeleteTargets();
        width = newWidth;
        height = newHeight;

        multisampleFramebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, multisampleFramebuffer);
        colorRenderbuffer = CreateRenderbuffer(RenderbufferStorage.Rgba8, sampleCount);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, colorRenderbuffer);
        depthRenderbuffer = CreateRenderbuffer(RenderbufferStorage.DepthComponent32f, sampleCount);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthRenderbuffer);
        CheckFramebuffer();

        resolveFramebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, resolveFramebuffer);
        resolveRenderbuffer = CreateRenderbuffer(RenderbufferStorage.Rgba8, 0);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, resolveRenderbuffer);
        CheckFramebuffer();

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    private int CreateRenderbuffer(RenderbufferStorage format, int samples)
    {
        int id = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, id);
        if (samples > 1)
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, samples, format, width, height);
        else
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, format, width, height);
        return id;
    }

    private static void CheckFramebuffer()
    {
        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
            throw new InvalidOperationException($"OpenGL could not create an off-screen image ({status}).");
    }

    private void EnsureGeometry(Mesh mesh, Color defaultColor)
    {
        if (ReferenceEquals(mesh, uploadedMesh) && defaultColor == uploadedColor)
            return;

        DeleteGeometry();
        var gpuMesh = GpuMesh.Build(mesh, defaultColor);
        Upload(0, gpuMesh.Triangles);
        Upload(1, gpuMesh.Edges);
        Upload(2, gpuMesh.Points);
        uploadedMesh = mesh;
        uploadedColor = defaultColor;
    }

    private void Upload(int slot, GpuVertex[] vertices)
    {
        vertexCounts[slot] = vertices.Length;
        if (vertices.Length == 0)
            return;

        vertexArrays[slot] = GL.GenVertexArray();
        GL.BindVertexArray(vertexArrays[slot]);
        vertexBuffers[slot] = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffers[slot]);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * GpuVertex.Size, vertices, BufferUsageHint.StaticDraw);
        for (int attribute = 0; attribute < 5; attribute++)
        {
            GL.EnableVertexAttribArray(attribute);
            GL.VertexAttribPointer(attribute, 3, VertexAttribPointerType.Float, false, GpuVertex.Size, attribute * 3 * sizeof(float));
        }
        GL.BindVertexArray(0);
    }

    private static int LinkProgram(string vertexSource, string fragmentSource)
    {
        int vertex = CompileShader(ShaderType.VertexShader, vertexSource);
        int fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);
        int program = GL.CreateProgram();
        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);
        GL.LinkProgram(program);
        GL.DetachShader(program, vertex);
        GL.DetachShader(program, fragment);
        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linked);
        if (linked == 0)
            throw new InvalidOperationException($"OpenGL shader link failed: {GL.GetProgramInfoLog(program)}");
        return program;
    }

    private static int CompileShader(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int compiled);
        if (compiled == 0)
            throw new InvalidOperationException($"OpenGL {type} failed to compile: {GL.GetShaderInfoLog(shader)}");
        return shader;
    }

    private void DeleteTargets()
    {
        if (multisampleFramebuffer != 0)
        {
            GL.DeleteFramebuffer(multisampleFramebuffer);
            GL.DeleteFramebuffer(resolveFramebuffer);
            GL.DeleteRenderbuffer(colorRenderbuffer);
            GL.DeleteRenderbuffer(depthRenderbuffer);
            GL.DeleteRenderbuffer(resolveRenderbuffer);
        }
        multisampleFramebuffer = resolveFramebuffer = colorRenderbuffer = depthRenderbuffer = resolveRenderbuffer = 0;
    }

    private void DeleteGeometry()
    {
        for (int slot = 0; slot < 3; slot++)
        {
            if (vertexArrays[slot] != 0)
            {
                GL.DeleteVertexArray(vertexArrays[slot]);
                GL.DeleteBuffer(vertexBuffers[slot]);
            }
            vertexArrays[slot] = vertexBuffers[slot] = vertexCounts[slot] = 0;
        }
        uploadedMesh = null;
    }

    public void Dispose()
    {
        window.MakeCurrent();
        DeleteTargets();
        DeleteGeometry();
        GL.DeleteVertexArray(emptyVertexArray);
        GL.DeleteBuffer(uniformBuffer);
        GL.DeleteProgram(sceneProgram);
        GL.DeleteProgram(backgroundProgram);
        window.Dispose();
    }

    private static string Vec3(Color c) =>
        string.Create(CultureInfo.InvariantCulture, $"vec3({c.R / 255f}, {c.G / 255f}, {c.B / 255f})");

    // GLSL translation of the HLSL in Direct3DRenderer; see the comments there.
    private const string Common = """
        #version 330 core
        layout(std140, row_major) uniform Frame
        {
            mat4  View;
            vec2  Focal;
            float Distance;
            float Near;
            vec3  LightDirection;
            float Far;
            float Ambient;
            float Diffuse;
            float SpecularStrength;
            float Shininess;
            vec2  PixelSize;
            int   Mode;
            float PointRadius;
            float DepthMin;
            float DepthMax;
            vec2  Padding;
        };

        const int ModePoints = 0;
        const int ModeWireframe = 1;
        const int ModeFlat = 2;
        const int ModeGouraud = 3;
        const int ModePhong = 4;

        vec3 Shade(vec3 normal, vec3 position, vec3 albedo, bool specular)
        {
            vec3 toEye = normalize(vec3(0.0, 0.0, Distance) - position);
            if (dot(normal, toEye) < 0.0)
                normal = -normal;
            float lambert = max(0.0, dot(normal, LightDirection));
            vec3 color = albedo * (Ambient + Diffuse * lambert);
            if (specular && lambert > 0.0)
            {
                vec3 halfway = normalize(LightDirection + toEye);
                color += vec3(pow(max(0.0, dot(normal, halfway)), Shininess) * SpecularStrength);
            }
            return color;
        }

        """;

    private const string SceneVertexShader = Common + """
        layout(location = 0) in vec3 inPosition;
        layout(location = 1) in vec3 inNormal;
        layout(location = 2) in vec3 inFaceNormal;
        layout(location = 3) in vec3 inCentroid;
        layout(location = 4) in vec3 inAlbedo;

        out vec3 vViewPosition;
        out vec3 vNormal;
        out vec3 vColor;
        out vec2 vCorner;

        // Same projection as the other renderers, but with +Y up instead of down: the picture comes out
        // upside down, which makes OpenGL's bottom-up rows read back in top-down order.
        vec4 Project(vec3 v)
        {
            float depth = Distance - v.z;
            float z = (depth - Near) * Far / (Far - Near);   // 0..depth
            return vec4(v.x * Focal.x, v.y * Focal.y, 2.0 * z - depth, depth);
        }

        vec3 DepthCue(vec3 albedo, vec3 v)
        {
            float nearness = clamp((1.0 / (Distance - v.z) - DepthMin) / (DepthMax - DepthMin), 0.0, 1.0);
            return sqrt(albedo) * (0.3 + 0.7 * nearness);
        }

        void main()
        {
            vec3 v = (vec4(inPosition, 1.0) * View).xyz;
            gl_Position = Project(v);
            vViewPosition = v;
            vNormal = (vec4(inNormal, 0.0) * View).xyz;
            vCorner = vec2(0.0);

            if (Mode == ModePoints)
            {
                float extent = PointRadius + 1.0;
                vCorner = inNormal.xy * extent;
                gl_Position.xy += inNormal.xy * extent * PixelSize * gl_Position.w;
                vColor = DepthCue(inAlbedo, v);
            }
            else if (Mode == ModeWireframe)
            {
                vColor = DepthCue(inAlbedo, v);
            }
            else if (Mode == ModeFlat)
            {
                vec3 faceCenter = (vec4(inCentroid, 1.0) * View).xyz;
                vec3 faceNormal = normalize((vec4(inFaceNormal, 0.0) * View).xyz);
                vColor = Shade(faceNormal, faceCenter, inAlbedo, false);
            }
            else if (Mode == ModeGouraud)
            {
                vColor = Shade(normalize(vNormal), v, inAlbedo, true);
            }
            else
            {
                vColor = inAlbedo;
            }
        }
        """;

    private const string SceneFragmentShader = Common + """
        in vec3 vViewPosition;
        in vec3 vNormal;
        in vec3 vColor;
        in vec2 vCorner;
        out vec4 outColor;

        void main()
        {
            if (Mode == ModePoints)
            {
                float alpha = clamp(PointRadius + 0.5 - length(vCorner), 0.0, 1.0);
                if (alpha <= 0.0)
                    discard;
                outColor = vec4(vColor, alpha);
                return;
            }
            if (Mode == ModeWireframe)
            {
                outColor = vec4(vColor, 1.0);
                return;
            }
            vec3 color = vColor;
            if (Mode == ModePhong)
                color = Shade(normalize(vNormal), vViewPosition, vColor, true);
            outColor = vec4(sqrt(clamp(color, 0.0, 1.0)), 1.0);
        }
        """;

    private const string BackgroundVertexShader = Common + """
        void main()
        {
            vec2 uv = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2);
            gl_Position = vec4(uv * 2.0 - 1.0, 0.0, 1.0);
        }
        """;

    // Row 0 of the upside-down picture is the top of the image.
    private static readonly string BackgroundFragmentShader = Common + $$"""
        out vec4 outColor;
        void main()
        {
            float t = clamp(gl_FragCoord.y * PixelSize.y * 0.5, 0.0, 1.0);
            outColor = vec4(mix({{Vec3(RenderConstants.BackgroundTop)}}, {{Vec3(RenderConstants.BackgroundBottom)}}, t), 1.0);
        }
        """;
}
