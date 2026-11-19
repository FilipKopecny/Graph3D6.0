using System.Globalization;
using System.Runtime.InteropServices;
using Graph3D.Geometry;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Color = System.Drawing.Color;
using Viewport = Vortice.Mathematics.Viewport;

namespace Graph3D.Rendering;

/// <summary>A graphics card that Direct3D can render on.</summary>
public sealed record GpuAdapterInfo(int Index, string Name, ulong DedicatedMemory)
{
    public string Label => DedicatedMemory >= 256UL << 20
        ? string.Create(CultureInfo.InvariantCulture, $"{Name} ({DedicatedMemory / (double)(1UL << 30):0.#} GB)")
        : Name;
}

/// <summary>
/// Renders on the GPU with Direct3D 11. The graphics card fills the triangles and keeps the depth
/// buffer; lighting runs in our own shaders, a translation of <see cref="SoftwareRenderer"/>.
/// The picture is drawn off-screen (4× multisampled) and copied back into the <see cref="FrameBuffer"/>.
/// </summary>
public sealed class Direct3DRenderer : IRenderer, IDisposable
{
    private readonly ID3D11Device device;
    private readonly ID3D11DeviceContext context;
    private readonly ID3D11VertexShader sceneVertexShader;
    private readonly ID3D11PixelShader scenePixelShader;
    private readonly ID3D11VertexShader backgroundVertexShader;
    private readonly ID3D11PixelShader backgroundPixelShader;
    private readonly ID3D11InputLayout inputLayout;
    private readonly ID3D11Buffer constantBuffer;
    private readonly ID3D11RasterizerState rasterizer;
    private readonly ID3D11DepthStencilState depthTest;
    private readonly ID3D11DepthStencilState noDepth;
    private readonly ID3D11BlendState alphaBlend;
    private readonly uint sampleCount;

    private ID3D11Texture2D? colorTexture, depthTexture, resolveTexture, stagingTexture;
    private ID3D11RenderTargetView? colorView;
    private ID3D11DepthStencilView? depthView;
    private int width, height;

    private Mesh? uploadedMesh;
    private Color uploadedColor;
    private ID3D11Buffer? triangleBuffer, edgeBuffer, pointBuffer;
    private int triangleCount, edgeCount, pointCount;

    private Direct3DRenderer(GpuAdapterInfo adapter, ID3D11Device device, ID3D11DeviceContext context)
    {
        Adapter = adapter;
        this.device = device;
        this.context = context;

        var vertexCode = Compiler.Compile(ShaderSource, "SceneVS", "graph3d.hlsl", "vs_5_0");
        sceneVertexShader = device.CreateVertexShader(vertexCode.Span);
        scenePixelShader = device.CreatePixelShader(Compiler.Compile(ShaderSource, "ScenePS", "graph3d.hlsl", "ps_5_0").Span);
        backgroundVertexShader = device.CreateVertexShader(Compiler.Compile(ShaderSource, "BackgroundVS", "graph3d.hlsl", "vs_5_0").Span);
        backgroundPixelShader = device.CreatePixelShader(Compiler.Compile(ShaderSource, "BackgroundPS", "graph3d.hlsl", "ps_5_0").Span);

        inputLayout = device.CreateInputLayout(
        [
            new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            new InputElementDescription("TEXCOORD", 0, Format.R32G32B32_Float, 24, 0),
            new InputElementDescription("TEXCOORD", 1, Format.R32G32B32_Float, 36, 0),
            new InputElementDescription("COLOR", 0, Format.R32G32B32_Float, 48, 0),
        ], vertexCode.Span);

        constantBuffer = device.CreateBuffer((uint)Marshal.SizeOf<FrameConstants>(), BindFlags.ConstantBuffer);

        var rasterizerDescription = RasterizerDescription.CullNone; // lit from both sides, like the software renderer
        rasterizerDescription.MultisampleEnable = true;
        rasterizerDescription.AntialiasedLineEnable = true;
        rasterizer = device.CreateRasterizerState(rasterizerDescription);
        depthTest = device.CreateDepthStencilState(DepthStencilDescription.Default);
        noDepth = device.CreateDepthStencilState(DepthStencilDescription.None);
        alphaBlend = device.CreateBlendState(BlendDescription.NonPremultiplied);

        sampleCount = device.CheckMultisampleQualityLevels(Format.B8G8R8A8_UNorm, 4) > 0 ? 4u : 1u;
    }

    public GpuAdapterInfo Adapter { get; }

    public string Name => $"Direct3D 11 · {Adapter.Name}";

    /// <summary>Hardware graphics adapters, in the order Windows reports them.</summary>
    public static IReadOnlyList<GpuAdapterInfo> EnumerateAdapters()
    {
        var adapters = new List<GpuAdapterInfo>();
        try
        {
            using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
            for (uint i = 0; factory.EnumAdapters1(i, out var adapter).Success; i++)
            {
                using (adapter)
                {
                    var description = adapter.Description1;
                    if ((description.Flags & AdapterFlags.Software) != 0)
                        continue; // "Microsoft Basic Render Driver" is a CPU fallback, not a GPU
                    adapters.Add(new GpuAdapterInfo((int)i, description.Description.Trim('\0', ' '), (ulong)description.DedicatedVideoMemory));
                }
            }
        }
        catch (Exception ex) when (ex is SharpGen.Runtime.SharpGenException or DllNotFoundException or EntryPointNotFoundException)
        {
            // No DXGI: the menu simply offers no Direct3D adapters.
        }
        return adapters;
    }

    /// <summary>Creates a renderer on the given adapter. Throws when Direct3D 11 is not available on it.</summary>
    public static Direct3DRenderer Create(GpuAdapterInfo adapterInfo)
    {
        using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
        factory.EnumAdapters1((uint)adapterInfo.Index, out var adapter).CheckError();
        using (adapter)
        {
            D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, DeviceCreationFlags.BgraSupport,
                [FeatureLevel.Level_11_0], out ID3D11Device device, out ID3D11DeviceContext context).CheckError();
            try
            {
                return new Direct3DRenderer(adapterInfo, device!, context!);
            }
            catch
            {
                context?.Dispose();
                device?.Dispose();
                throw;
            }
        }
    }

    public void Render(Mesh mesh, Camera camera, FrameBuffer target, RenderSettings settings)
    {
        EnsureTargets(target.Width, target.Height);
        EnsureGeometry(mesh, settings.BaseColor);

        var frame = FrameConstants.Create(mesh, camera, width, height, settings);
        context.UpdateSubresource(in frame, constantBuffer);

        context.ClearState();
        context.ClearRenderTargetView(colorView!, new Color4(0, 0, 0, 1));
        context.ClearDepthStencilView(depthView!, DepthStencilClearFlags.Depth, 1f, 0);
        context.OMSetRenderTargets(colorView!, depthView);
        context.RSSetViewport(new Viewport(0, 0, width, height));
        context.RSSetState(rasterizer);
        context.VSSetConstantBuffer(0, constantBuffer);
        context.PSSetConstantBuffer(0, constantBuffer);
        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Background gradient: one triangle that covers the screen.
        context.OMSetDepthStencilState(noDepth);
        context.VSSetShader(backgroundVertexShader);
        context.PSSetShader(backgroundPixelShader);
        context.Draw(3, 0);

        var (buffer, count, topology) = settings.Mode switch
        {
            RenderMode.Points => (pointBuffer, pointCount, PrimitiveTopology.TriangleList),
            RenderMode.Wireframe => (edgeBuffer, edgeCount, PrimitiveTopology.LineList),
            _ => (triangleBuffer, triangleCount, PrimitiveTopology.TriangleList),
        };
        if (buffer is not null && count > 0)
        {
            context.OMSetDepthStencilState(depthTest);
            if (settings.Mode == RenderMode.Points)
                context.OMSetBlendState(alphaBlend); // soft edges of the round dots
            context.IASetInputLayout(inputLayout);
            context.IASetPrimitiveTopology(topology);
            context.IASetVertexBuffer(0, buffer, GpuVertex.Size, 0);
            context.VSSetShader(sceneVertexShader);
            context.PSSetShader(scenePixelShader);
            context.Draw((uint)count, 0);
        }

        ReadBack(target);
    }

    private void ReadBack(FrameBuffer target)
    {
        if (sampleCount > 1)
            context.ResolveSubresource(resolveTexture!, 0, colorTexture!, 0, Format.B8G8R8A8_UNorm);
        context.CopyResource(stagingTexture!, resolveTexture ?? colorTexture!);

        context.Map(stagingTexture!, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None, out MappedSubresource mapped).CheckError();
        try
        {
            // B8G8R8A8 in memory is exactly the 0xAARRGGBB integers of the frame buffer.
            for (int y = 0; y < height; y++)
                Marshal.Copy(mapped.DataPointer + y * (nint)mapped.RowPitch, target.Pixels, y * width, width);
        }
        finally
        {
            context.Unmap(stagingTexture!, 0);
        }
    }

    private void EnsureTargets(int newWidth, int newHeight)
    {
        if (colorTexture is not null && newWidth == width && newHeight == height)
            return;

        DisposeTargets();
        width = newWidth;
        height = newHeight;

        var description = new Texture2DDescription(Format.B8G8R8A8_UNorm, (uint)width, (uint)height, 1, 1,
            BindFlags.RenderTarget, ResourceUsage.Default, CpuAccessFlags.None, sampleCount, 0);
        colorTexture = device.CreateTexture2D(description);
        colorView = device.CreateRenderTargetView(colorTexture);

        depthTexture = device.CreateTexture2D(new Texture2DDescription(Format.D32_Float, (uint)width, (uint)height, 1, 1,
            BindFlags.DepthStencil, ResourceUsage.Default, CpuAccessFlags.None, sampleCount, 0));
        depthView = device.CreateDepthStencilView(depthTexture);

        if (sampleCount > 1)
            resolveTexture = device.CreateTexture2D(new Texture2DDescription(Format.B8G8R8A8_UNorm, (uint)width, (uint)height, 1, 1,
                BindFlags.None, ResourceUsage.Default, CpuAccessFlags.None, 1, 0));

        stagingTexture = device.CreateTexture2D(new Texture2DDescription(Format.B8G8R8A8_UNorm, (uint)width, (uint)height, 1, 1,
            BindFlags.None, ResourceUsage.Staging, CpuAccessFlags.Read, 1, 0));
    }

    private void EnsureGeometry(Mesh mesh, Color defaultColor)
    {
        if (ReferenceEquals(mesh, uploadedMesh) && defaultColor == uploadedColor)
            return;

        DisposeGeometry();
        var gpuMesh = GpuMesh.Build(mesh, defaultColor);
        triangleBuffer = CreateVertexBuffer(gpuMesh.Triangles, out triangleCount);
        edgeBuffer = CreateVertexBuffer(gpuMesh.Edges, out edgeCount);
        pointBuffer = CreateVertexBuffer(gpuMesh.Points, out pointCount);
        uploadedMesh = mesh;
        uploadedColor = defaultColor;
    }

    private ID3D11Buffer? CreateVertexBuffer(GpuVertex[] vertices, out int count)
    {
        count = vertices.Length;
        return count == 0 ? null : device.CreateBuffer(vertices, BindFlags.VertexBuffer);
    }

    private void DisposeTargets()
    {
        colorView?.Dispose();
        depthView?.Dispose();
        colorTexture?.Dispose();
        depthTexture?.Dispose();
        resolveTexture?.Dispose();
        stagingTexture?.Dispose();
        colorView = null;
        depthView = null;
        colorTexture = depthTexture = resolveTexture = stagingTexture = null;
    }

    private void DisposeGeometry()
    {
        triangleBuffer?.Dispose();
        edgeBuffer?.Dispose();
        pointBuffer?.Dispose();
        triangleBuffer = edgeBuffer = pointBuffer = null;
        uploadedMesh = null;
    }

    public void Dispose()
    {
        DisposeTargets();
        DisposeGeometry();
        alphaBlend.Dispose();
        noDepth.Dispose();
        depthTest.Dispose();
        rasterizer.Dispose();
        constantBuffer.Dispose();
        inputLayout.Dispose();
        backgroundPixelShader.Dispose();
        backgroundVertexShader.Dispose();
        scenePixelShader.Dispose();
        sceneVertexShader.Dispose();
        context.ClearState();
        context.Flush();
        context.Dispose();
        device.Dispose();
    }

    private static string Float3(Color c) =>
        string.Create(CultureInfo.InvariantCulture, $"float3({c.R / 255f}, {c.G / 255f}, {c.B / 255f})");

    private static readonly string ShaderSource = $$"""
        // Graph3D scene shaders (HLSL). Mirrors SoftwareRenderer: same projection, lighting and depth cueing.
        cbuffer Frame : register(b0)
        {
            row_major float4x4 View;
            float2 Focal;
            float  Distance;
            float  Near;
            float3 LightDirection;
            float  Far;
            float  Ambient;
            float  Diffuse;
            float  SpecularStrength;
            float  Shininess;
            float2 PixelSize;
            int    Mode;
            float  PointRadius;
            float  DepthMin;
            float  DepthMax;
            float2 Padding;
        };

        static const int ModePoints = 0;
        static const int ModeWireframe = 1;
        static const int ModeFlat = 2;
        static const int ModeGouraud = 3;
        static const int ModePhong = 4;
        static const float3 BackgroundTop = {{Float3(RenderConstants.BackgroundTop)}};
        static const float3 BackgroundBottom = {{Float3(RenderConstants.BackgroundBottom)}};

        struct VSInput
        {
            float3 Position   : POSITION;
            float3 Normal     : NORMAL;
            float3 FaceNormal : TEXCOORD0;
            float3 Centroid   : TEXCOORD1;
            float3 Albedo     : COLOR0;
        };

        struct PSInput
        {
            float4 Position     : SV_Position;
            float3 ViewPosition : TEXCOORD0;
            float3 Normal       : TEXCOORD1;
            float3 Color        : COLOR0;
            float2 Corner       : TEXCOORD2;
        };

        // View space: +X right, +Y down, the eye sits at (0, 0, Distance) looking towards -Z.
        float4 Project(float3 v)
        {
            float depth = Distance - v.z;
            return float4(v.x * Focal.x, -v.y * Focal.y, (depth - Near) * Far / (Far - Near), depth);
        }

        // Blinn-Phong in linear colour space, lit from both sides.
        float3 Shade(float3 normal, float3 position, float3 albedo, bool specular)
        {
            float3 toEye = normalize(float3(0, 0, Distance) - position);
            if (dot(normal, toEye) < 0)
                normal = -normal;
            float lambert = max(0, dot(normal, LightDirection));
            float3 color = albedo * (Ambient + Diffuse * lambert);
            if (specular && lambert > 0)
            {
                float3 halfway = normalize(LightDirection + toEye);
                color += pow(max(0, dot(normal, halfway)), Shininess) * SpecularStrength;
            }
            return color;
        }

        // Nearer points and lines are brighter. Returns an sRGB colour.
        float3 DepthCue(float3 albedo, float3 v)
        {
            float nearness = saturate((1.0 / (Distance - v.z) - DepthMin) / (DepthMax - DepthMin));
            return sqrt(albedo) * (0.3 + 0.7 * nearness);
        }

        PSInput SceneVS(VSInput input)
        {
            PSInput output;
            float3 v = mul(float4(input.Position, 1), View).xyz;
            output.Position = Project(v);
            output.ViewPosition = v;
            output.Normal = mul(float4(input.Normal, 0), View).xyz;
            output.Corner = 0;

            if (Mode == ModePoints)
            {
                // Grow the vertex into a small square that always covers the same number of pixels.
                float extent = PointRadius + 1;
                output.Corner = input.Normal.xy * extent;
                output.Position.xy += input.Normal.xy * extent * PixelSize * output.Position.w;
                output.Color = DepthCue(input.Albedo, v);
            }
            else if (Mode == ModeWireframe)
            {
                output.Color = DepthCue(input.Albedo, v);
            }
            else if (Mode == ModeFlat)
            {
                float3 faceCenter = mul(float4(input.Centroid, 1), View).xyz;
                float3 faceNormal = normalize(mul(float4(input.FaceNormal, 0), View).xyz);
                output.Color = Shade(faceNormal, faceCenter, input.Albedo, false);
            }
            else if (Mode == ModeGouraud)
            {
                output.Color = Shade(normalize(output.Normal), v, input.Albedo, true);
            }
            else
            {
                output.Color = input.Albedo; // Phong: lit per pixel below
            }
            return output;
        }

        float4 ScenePS(PSInput input) : SV_Target
        {
            if (Mode == ModePoints)
            {
                float alpha = saturate(PointRadius + 0.5 - length(input.Corner));
                if (alpha <= 0)
                    discard;
                return float4(input.Color, alpha);
            }
            if (Mode == ModeWireframe)
                return float4(input.Color, 1);

            float3 color = input.Color;
            if (Mode == ModePhong)
                color = Shade(normalize(input.Normal), input.ViewPosition, input.Color, true);
            return float4(sqrt(saturate(color)), 1); // linear → sRGB (gamma 2), as in the software renderer
        }

        float4 BackgroundVS(uint id : SV_VertexID) : SV_Position
        {
            float2 uv = float2((id << 1) & 2, id & 2);
            return float4(uv * float2(2, -2) + float2(-1, 1), 0, 1);
        }

        float4 BackgroundPS(float4 position : SV_Position) : SV_Target
        {
            float t = saturate(position.y * PixelSize.y * 0.5);
            return float4(lerp(BackgroundTop, BackgroundBottom, t), 1);
        }
        """;
}
