using System.Drawing.Drawing2D;
using System.Numerics;
using Graph3D.Geometry;

namespace Graph3D.Rendering;

/// <summary>
/// Software renderer. Filled modes use a z-buffered, perspective-correct triangle rasterizer that
/// runs on several threads (each thread owns a horizontal band of the screen). Lighting is a
/// headlight-style Blinn–Phong model evaluated in linear colour space; surfaces are lit from both sides.
/// </summary>
public sealed class SoftwareRenderer : IRenderer
{
    private const float NearPlane = RenderConstants.NearPlane;
    private static Color BackgroundTop => RenderConstants.BackgroundTop;
    private static Color BackgroundBottom => RenderConstants.BackgroundBottom;

    public string Name => "Software (CPU)";

    private Vector3[] viewPositions = [];
    private Vector3[] viewNormals = [];
    private Vector3[] screen = []; // x, y in pixels; z = 1 / depth (0 when behind the camera)
    private Vector3[] albedo = [];       // linear surface colour per vertex
    private Vector3[] vertexColors = []; // lit colour per vertex (Gouraud)
    private PreparedTriangle[] prepared = [];

    public void Render(Mesh mesh, Camera camera, FrameBuffer target, RenderSettings settings)
    {
        FillBackground(target);
        Array.Clear(target.Depth);

        int vertexCount = mesh.Positions.Length;
        if (vertexCount == 0)
            return;
        EnsureCapacity(ref viewPositions, vertexCount);
        EnsureCapacity(ref viewNormals, vertexCount);
        EnsureCapacity(ref screen, vertexCount);
        EnsureCapacity(ref albedo, vertexCount);

        for (int i = 0; i < vertexCount; i++)
            albedo[i] = ToLinear(ColorOf(mesh, i, settings));

        var rotation = camera.Rotation;
        float focal = target.Height * 0.5f / MathF.Tan(camera.FieldOfView * 0.5f);
        float centerX = target.Width * 0.5f, centerY = target.Height * 0.5f;

        for (int i = 0; i < vertexCount; i++)
        {
            var v = camera.ToView(mesh.Positions[i], rotation);
            viewPositions[i] = v;
            viewNormals[i] = Vector3.TransformNormal(mesh.Normals[i], rotation);
            float depth = camera.Distance - v.Z;
            if (depth < NearPlane)
            {
                screen[i] = default;
                continue;
            }
            float inv = 1f / depth;
            screen[i] = new Vector3(centerX + focal * v.X * inv, centerY + focal * v.Y * inv, inv);
        }

        var lighting = new Lighting(settings, new Vector3(0, 0, camera.Distance));

        switch (settings.Mode)
        {
            case RenderMode.Points:
                DrawPoints(mesh, target, settings);
                break;
            case RenderMode.Wireframe:
                DrawWireframe(mesh, target, settings);
                break;
            default:
                DrawFilled(mesh, target, settings.Mode, lighting);
                break;
        }
    }

    private static void EnsureCapacity<T>(ref T[] array, int count)
    {
        if (array.Length < count)
            array = new T[count];
    }

    /// <summary>The vertex's own colour, or the default colour when its object has none.</summary>
    private static Color ColorOf(Mesh mesh, int vertex, RenderSettings settings)
    {
        var color = mesh.Colors[vertex];
        return color.IsEmpty ? settings.BaseColor : color;
    }

    /// <summary>sRGB colour → linear (gamma 2 approximation, the inverse of <see cref="Pack"/>).</summary>
    private static Vector3 ToLinear(Color c)
    {
        var v = new Vector3(c.R, c.G, c.B) / 255f;
        return v * v;
    }

    private static void FillBackground(FrameBuffer target)
    {
        int w = target.Width, h = target.Height;
        var pixels = target.Pixels;
        for (int y = 0; y < h; y++)
        {
            float t = h > 1 ? (float)y / (h - 1) : 0;
            int color = Color.FromArgb(
                Lerp(BackgroundTop.R, BackgroundBottom.R, t),
                Lerp(BackgroundTop.G, BackgroundBottom.G, t),
                Lerp(BackgroundTop.B, BackgroundBottom.B, t)).ToArgb();
            pixels.AsSpan(y * w, w).Fill(color);
        }
    }

    private static int Lerp(int a, int b, float t) => (int)(a + (b - a) * t + 0.5f);

    /// <summary>Colour for a depth-cued point or line: near → full colour, far → dimmed.</summary>
    private static Color DepthCue(Color baseColor, float nearness)
    {
        float k = 0.3f + 0.7f * Math.Clamp(nearness, 0, 1);
        return Color.FromArgb((int)(baseColor.R * k), (int)(baseColor.G * k), (int)(baseColor.B * k));
    }

    private (float Min, float Max) DepthRange(int vertexCount)
    {
        float min = float.MaxValue, max = float.MinValue;
        for (int i = 0; i < vertexCount; i++)
        {
            float z = screen[i].Z;
            if (z <= 0)
                continue;
            min = MathF.Min(min, z);
            max = MathF.Max(max, z);
        }
        return (min, MathF.Max(max, min + 1e-12f));
    }

    private void DrawPoints(Mesh mesh, FrameBuffer target, RenderSettings settings)
    {
        int n = mesh.Positions.Length;
        var (min, max) = DepthRange(n);
        using var g = Graphics.FromImage(target.Bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(settings.BaseColor);
        const float size = RenderConstants.PointSize;
        for (int i = 0; i < n; i++)
        {
            var s = screen[i];
            if (s.Z <= 0)
                continue;
            brush.Color = DepthCue(ColorOf(mesh, i, settings), (s.Z - min) / (max - min));
            g.FillEllipse(brush, s.X - size / 2, s.Y - size / 2, size, size);
        }
    }

    private void DrawWireframe(Mesh mesh, FrameBuffer target, RenderSettings settings)
    {
        var (min, max) = DepthRange(mesh.Positions.Length);

        // Draw far edges first so nearer edges stay on top.
        var order = new List<(float Depth, Edge Edge)>(mesh.Edges.Length);
        foreach (var e in mesh.Edges)
        {
            float a = screen[e.A].Z, b = screen[e.B].Z;
            if (a > 0 && b > 0)
                order.Add(((a + b) * 0.5f, e));
        }
        order.Sort((x, y) => x.Depth.CompareTo(y.Depth));

        using var g = Graphics.FromImage(target.Bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(settings.BaseColor, RenderConstants.LineWidth);
        foreach (var (depth, e) in order)
        {
            pen.Color = DepthCue(ColorOf(mesh, e.A, settings), (depth - min) / (max - min));
            var a = screen[e.A];
            var b = screen[e.B];
            g.DrawLine(pen, a.X, a.Y, b.X, b.Y);
        }
    }

    private struct PreparedTriangle
    {
        public Vector3 S0, S1, S2; // screen x, y, 1/depth
        public Vector3 A0, A1, A2; // colour (flat/Gouraud) or normal (Phong)
        public Vector3 P0, P1, P2; // view-space position (Phong)
        public Vector3 Albedo;     // surface colour (Phong)
        public int MinY, MaxY;
    }

    private void DrawFilled(Mesh mesh, FrameBuffer target, RenderMode mode, Lighting lighting)
    {
        var triangles = mesh.Triangles;
        EnsureCapacity(ref prepared, triangles.Length);

        if (mode == RenderMode.Gouraud)
        {
            EnsureCapacity(ref vertexColors, mesh.Positions.Length);
            for (int i = 0; i < mesh.Positions.Length; i++)
                vertexColors[i] = lighting.Shade(viewNormals[i], viewPositions[i], albedo[i], specular: true);
        }

        int count = 0;
        foreach (var t in triangles)
        {
            Vector3 s0 = screen[t.A], s1 = screen[t.B], s2 = screen[t.C];
            if (s0.Z <= 0 || s1.Z <= 0 || s2.Z <= 0)
                continue; // (partly) behind the camera

            float area = (s1.X - s0.X) * (s2.Y - s0.Y) - (s2.X - s0.X) * (s1.Y - s0.Y);
            if (MathF.Abs(area) < 1e-6f)
                continue;

            Vector3 p0 = viewPositions[t.A], p1 = viewPositions[t.B], p2 = viewPositions[t.C];
            var faceNormal = Vector3.Cross(p1 - p0, p2 - p0);
            float length = faceNormal.Length();
            if (length < 1e-20f)
                continue;
            faceNormal /= length;

            ref var pt = ref prepared[count++];
            pt.S0 = s0;
            pt.S1 = s1;
            pt.S2 = s2;
            pt.MinY = (int)MathF.Floor(MathF.Min(s0.Y, MathF.Min(s1.Y, s2.Y)));
            pt.MaxY = (int)MathF.Ceiling(MathF.Max(s0.Y, MathF.Max(s1.Y, s2.Y)));

            var surface = albedo[t.A]; // all vertices of a triangle belong to the same object
            switch (mode)
            {
                case RenderMode.Flat:
                    pt.A0 = pt.A1 = pt.A2 = lighting.Shade(faceNormal, (p0 + p1 + p2) / 3f, surface, specular: false);
                    break;

                case RenderMode.Gouraud when t.Smooth:
                    pt.A0 = vertexColors[t.A];
                    pt.A1 = vertexColors[t.B];
                    pt.A2 = vertexColors[t.C];
                    break;

                case RenderMode.Gouraud:
                    pt.A0 = lighting.Shade(faceNormal, p0, surface, specular: true);
                    pt.A1 = lighting.Shade(faceNormal, p1, surface, specular: true);
                    pt.A2 = lighting.Shade(faceNormal, p2, surface, specular: true);
                    break;

                default: // Phong
                    pt.A0 = t.Smooth ? viewNormals[t.A] : faceNormal;
                    pt.A1 = t.Smooth ? viewNormals[t.B] : faceNormal;
                    pt.A2 = t.Smooth ? viewNormals[t.C] : faceNormal;
                    pt.P0 = p0;
                    pt.P1 = p1;
                    pt.P2 = p2;
                    pt.Albedo = surface;
                    break;
            }
        }

        bool perPixel = mode == RenderMode.Phong;
        int bands = Math.Clamp(Environment.ProcessorCount * 2, 1, Math.Max(1, target.Height / 8));
        int bandHeight = (target.Height + bands - 1) / bands;
        var tris = prepared;

        Parallel.For(0, bands, band =>
        {
            int y0 = band * bandHeight;
            int y1 = Math.Min(target.Height, y0 + bandHeight);
            for (int i = 0; i < count; i++)
            {
                ref readonly var t = ref tris[i];
                if (t.MaxY < y0 || t.MinY >= y1)
                    continue;
                Rasterize(in t, target, y0, y1, perPixel, lighting);
            }
        });
    }

    private static void Rasterize(in PreparedTriangle t, FrameBuffer target, int bandStart, int bandEnd, bool perPixel, in Lighting lighting)
    {
        float x0 = t.S0.X, y0 = t.S0.Y, z0 = t.S0.Z;
        float x1 = t.S1.X, y1 = t.S1.Y, z1 = t.S1.Z;
        float x2 = t.S2.X, y2 = t.S2.Y, z2 = t.S2.Z;

        float area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
        float invArea = 1f / area; // signed, so either winding works

        int width = target.Width;
        int minX = Math.Max(0, (int)MathF.Floor(MathF.Min(x0, MathF.Min(x1, x2))));
        int maxX = Math.Min(width - 1, (int)MathF.Ceiling(MathF.Max(x0, MathF.Max(x1, x2))));
        int minY = Math.Max(bandStart, t.MinY);
        int maxY = Math.Min(bandEnd - 1, t.MaxY);
        if (minX > maxX || minY > maxY)
            return;

        // Barycentric weights change linearly along a row.
        float dw0 = -(y2 - y1) * invArea;
        float dw1 = -(y0 - y2) * invArea;
        float dw2 = -(y1 - y0) * invArea;

        var pixels = target.Pixels;
        var depth = target.Depth;
        const float epsilon = -1e-5f;

        for (int y = minY; y <= maxY; y++)
        {
            float py = y + 0.5f;
            float px = minX + 0.5f;
            float w0 = ((x2 - x1) * (py - y1) - (y2 - y1) * (px - x1)) * invArea;
            float w1 = ((x0 - x2) * (py - y2) - (y0 - y2) * (px - x2)) * invArea;
            float w2 = ((x1 - x0) * (py - y0) - (y1 - y0) * (px - x0)) * invArea;
            int row = y * width;

            for (int x = minX; x <= maxX; x++, w0 += dw0, w1 += dw1, w2 += dw2)
            {
                if (w0 < epsilon || w1 < epsilon || w2 < epsilon)
                    continue;

                float z = w0 * z0 + w1 * z1 + w2 * z2; // 1/depth is linear in screen space
                int index = row + x;
                if (z <= depth[index])
                    continue;
                depth[index] = z;

                // Perspective-correct weights for the attributes.
                float invZ = 1f / z;
                float b0 = w0 * z0 * invZ, b1 = w1 * z1 * invZ, b2 = w2 * z2 * invZ;

                Vector3 color;
                if (perPixel)
                {
                    var normal = Vector3.Normalize(b0 * t.A0 + b1 * t.A1 + b2 * t.A2);
                    var position = b0 * t.P0 + b1 * t.P1 + b2 * t.P2;
                    color = lighting.Shade(normal, position, t.Albedo, specular: true);
                }
                else
                {
                    color = b0 * t.A0 + b1 * t.A1 + b2 * t.A2;
                }

                pixels[index] = Pack(color);
            }
        }
    }

    /// <summary>Linear colour → opaque sRGB pixel (gamma 2 approximation).</summary>
    private static int Pack(Vector3 c) =>
        unchecked((int)0xFF000000) | (ToByte(c.X) << 16) | (ToByte(c.Y) << 8) | ToByte(c.Z);

    private static int ToByte(float linear) =>
        linear <= 0 ? 0 : linear >= 1 ? 255 : (int)(MathF.Sqrt(linear) * 255f + 0.5f);

    private readonly struct Lighting
    {
        private readonly Vector3 lightDirection;
        private readonly Vector3 eye;
        private readonly float ambient, diffuse, specular, shininess;

        public Lighting(RenderSettings settings, Vector3 eye)
        {
            lightDirection = RenderConstants.LightDirection;
            this.eye = eye;
            ambient = settings.Ambient;
            diffuse = settings.Diffuse;
            specular = settings.Specular;
            shininess = settings.Shininess;
        }

        /// <param name="albedo">Linear surface colour.</param>
        public Vector3 Shade(Vector3 normal, Vector3 position, Vector3 albedo, bool specular)
        {
            var toEye = Vector3.Normalize(eye - position);
            if (Vector3.Dot(normal, toEye) < 0)
                normal = -normal; // two-sided lighting

            float lambert = MathF.Max(0, Vector3.Dot(normal, lightDirection));
            var color = albedo * (ambient + diffuse * lambert);

            if (specular && lambert > 0)
            {
                var half = Vector3.Normalize(lightDirection + toEye);
                float highlight = MathF.Pow(MathF.Max(0, Vector3.Dot(normal, half)), shininess) * this.specular;
                color += new Vector3(highlight);
            }
            return color;
        }
    }
}
