using System.Numerics;
using System.Runtime.InteropServices;
using Graph3D.Geometry;

namespace Graph3D.Rendering;

/// <summary>
/// One vertex as the GPU renderers send it to the graphics card. Triangles are not indexed: every
/// corner gets its own copy, so a corner can carry its triangle's face normal and centre as well.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct GpuVertex
{
    public const int Size = 15 * sizeof(float);

    public Vector3 Position;   // world space
    public Vector3 Normal;     // shading normal (world); for points: the corner of the quad
    public Vector3 FaceNormal; // flat shading
    public Vector3 Centroid;   // flat shading
    public Vector3 Albedo;     // linear surface colour
}

/// <summary>The three vertex lists a GPU renderer needs, built from a <see cref="Mesh"/>.</summary>
internal sealed class GpuMesh
{
    private GpuMesh(GpuVertex[] triangles, GpuVertex[] edges, GpuVertex[] points)
    {
        Triangles = triangles;
        Edges = edges;
        Points = points;
    }

    /// <summary>Three vertices per triangle.</summary>
    public GpuVertex[] Triangles { get; }

    /// <summary>Two vertices per edge.</summary>
    public GpuVertex[] Edges { get; }

    /// <summary>Six vertices (a two-triangle square) per mesh vertex, expanded to a dot in the vertex shader.</summary>
    public GpuVertex[] Points { get; }

    public static GpuMesh Build(Mesh mesh, Color defaultColor)
    {
        var albedo = new Vector3[mesh.Positions.Length];
        for (int i = 0; i < albedo.Length; i++)
        {
            var c = mesh.Colors[i].IsEmpty ? defaultColor : mesh.Colors[i];
            var v = new Vector3(c.R, c.G, c.B) / 255f;
            albedo[i] = v * v; // sRGB → linear, the same approximation as the software renderer
        }

        var triangles = new GpuVertex[mesh.Triangles.Length * 3];
        int n = 0;
        foreach (var t in mesh.Triangles)
        {
            Vector3 a = mesh.Positions[t.A], b = mesh.Positions[t.B], c = mesh.Positions[t.C];
            var face = Vector3.Cross(b - a, c - a);
            float length = face.Length();
            face = length > 1e-20f ? face / length : Vector3.UnitZ;
            var centroid = (a + b + c) / 3f;
            foreach (int index in (ReadOnlySpan<int>)[t.A, t.B, t.C])
            {
                triangles[n++] = new GpuVertex
                {
                    Position = mesh.Positions[index],
                    Normal = t.Smooth ? mesh.Normals[index] : face,
                    FaceNormal = face,
                    Centroid = centroid,
                    Albedo = albedo[t.A], // all corners of a triangle belong to the same object
                };
            }
        }

        var edges = new GpuVertex[mesh.Edges.Length * 2];
        n = 0;
        foreach (var e in mesh.Edges)
        {
            edges[n++] = new GpuVertex { Position = mesh.Positions[e.A], Albedo = albedo[e.A] };
            edges[n++] = new GpuVertex { Position = mesh.Positions[e.B], Albedo = albedo[e.A] };
        }

        ReadOnlySpan<Vector2> corners = [new(-1, -1), new(1, -1), new(1, 1), new(-1, -1), new(1, 1), new(-1, 1)];
        var points = new GpuVertex[mesh.Positions.Length * corners.Length];
        n = 0;
        for (int i = 0; i < mesh.Positions.Length; i++)
            foreach (var corner in corners)
                points[n++] = new GpuVertex { Position = mesh.Positions[i], Normal = new Vector3(corner, 0), Albedo = albedo[i] };

        return new GpuMesh(triangles, edges, points);
    }
}

/// <summary>Per-frame values shared by the Direct3D and OpenGL shaders (std140 / cbuffer compatible layout).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct FrameConstants
{
    public Matrix4x4 View;           // world → view space (row-vector convention)
    public Vector2 Focal;            // 2·focal / width, 2·focal / height
    public float Distance;           // eye position is (0, 0, Distance) in view space
    public float Near;
    public Vector3 LightDirection;
    public float Far;
    public float Ambient;
    public float Diffuse;
    public float Specular;
    public float Shininess;
    public Vector2 PixelSize;        // size of one pixel in normalised device units
    public int Mode;                 // RenderMode
    public float PointRadius;        // pixels
    public float DepthMin;           // 1 / depth of the farthest vertex (depth cueing)
    public float DepthMax;           // 1 / depth of the nearest vertex
    public Vector2 Padding;

    /// <summary>Everything that depends on the camera, the window size and the settings.</summary>
    public static FrameConstants Create(Mesh mesh, Camera camera, int width, int height, RenderSettings settings)
    {
        var rotation = camera.Rotation;
        float minInverse = float.MaxValue, maxInverse = float.MinValue, far = 1f;
        foreach (var p in mesh.Positions)
        {
            float depth = camera.Distance - camera.ToView(p, rotation).Z;
            if (depth < RenderConstants.NearPlane)
                continue;
            far = MathF.Max(far, depth);
            minInverse = MathF.Min(minInverse, 1f / depth);
            maxInverse = MathF.Max(maxInverse, 1f / depth);
        }
        if (minInverse > maxInverse)
            (minInverse, maxInverse) = (0f, 1f);

        float focal = height * 0.5f / MathF.Tan(camera.FieldOfView * 0.5f);
        return new FrameConstants
        {
            View = Matrix4x4.CreateTranslation(-camera.Target) * rotation * Matrix4x4.CreateTranslation(camera.Pan.X, camera.Pan.Y, 0),
            Focal = new Vector2(2f * focal / width, 2f * focal / height),
            Distance = camera.Distance,
            Near = RenderConstants.NearPlane,
            Far = far * 1.01f + 0.1f,
            LightDirection = RenderConstants.LightDirection,
            Ambient = settings.Ambient,
            Diffuse = settings.Diffuse,
            Specular = settings.Specular,
            Shininess = settings.Shininess,
            PixelSize = new Vector2(2f / width, 2f / height),
            Mode = (int)settings.Mode,
            PointRadius = RenderConstants.PointSize * 0.5f,
            DepthMin = minInverse,
            DepthMax = MathF.Max(maxInverse, minInverse + 1e-12f),
        };
    }
}
