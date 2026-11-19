using System.Numerics;

namespace Graph3D.Geometry;

/// <summary>A triangle. Smooth triangles use interpolated vertex normals; the others are shaded with their face normal (sharp edges, e.g. boxes).</summary>
public readonly record struct Triangle(int A, int B, int C, bool Smooth);

/// <summary>A wireframe edge between two vertices.</summary>
public readonly record struct Edge(int A, int B);

/// <summary>Immutable triangle mesh in world space.</summary>
public sealed class Mesh
{
    public Mesh(Vector3[] positions, Vector3[] normals, Color[] colors, Triangle[] triangles, Edge[] edges)
    {
        Positions = positions;
        Normals = normals;
        Colors = colors;
        Triangles = triangles;
        Edges = edges;

        if (positions.Length == 0)
            return;

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var p in positions)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        BoundsCenter = (min + max) * 0.5f;
        foreach (var p in positions)
            BoundsRadius = MathF.Max(BoundsRadius, Vector3.Distance(p, BoundsCenter));
    }

    public Vector3[] Positions { get; }

    /// <summary>Per-vertex normals averaged from adjacent smooth triangles.</summary>
    public Vector3[] Normals { get; }

    /// <summary>Per-vertex surface colour; <see cref="Color.Empty"/> means "use the renderer's default colour".</summary>
    public Color[] Colors { get; }

    public Triangle[] Triangles { get; }

    public Edge[] Edges { get; }

    public Vector3 BoundsCenter { get; }

    public float BoundsRadius { get; }
}
