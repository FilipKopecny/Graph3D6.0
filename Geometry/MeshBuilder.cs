using System.Numerics;
using Graph3D.Scene;

namespace Graph3D.Geometry;

/// <summary>Turns the objects of a scene into one triangle mesh.</summary>
public sealed class MeshBuilder
{
    private readonly List<Vector3> positions = [];
    private readonly List<Color> colors = [];
    private readonly List<Triangle> triangles = [];
    private readonly HashSet<Edge> edges = [];

    /// <param name="patchResolution">Grid steps per spline segment for B-spline and Bézier surfaces.</param>
    public static Mesh Build(SceneDescription scene, int patchResolution = 10)
    {
        var builder = new MeshBuilder();
        foreach (var primitive in scene.Primitives)
            builder.Add(primitive, patchResolution);
        return builder.ToMesh();
    }

    private void Add(Primitive primitive, int patchResolution)
    {
        int first = positions.Count;

        switch (primitive)
        {
            case BoxDef box:
                AddBox(box.Size * 0.5f);
                break;
            case TorusDef torus:
                AddTorus(torus);
                break;
            case BSplinePatchDef patch:
                AddPatch(patch.ControlPoints, PatchBasis.BSpline, patchResolution);
                break;
            case BezierPatchDef patch:
                AddPatch(patch.ControlPoints, PatchBasis.Bezier, patchResolution);
                break;
            default:
                throw new NotSupportedException(primitive.GetType().Name);
        }

        for (int i = first; i < positions.Count; i++)
        {
            positions[i] = Transform(positions[i], primitive.Rotation, primitive.Position);
            colors.Add(primitive.Color);
        }
    }

    /// <summary>Rotates about Z, then Y, then X and translates, matching the original program's formulas.</summary>
    private static Vector3 Transform(Vector3 p, Vector3 rotation, Vector3 translation)
    {
        float c = MathF.Cos(rotation.Z), s = MathF.Sin(rotation.Z);
        p = new Vector3(p.X * c - p.Y * s, p.X * s + p.Y * c, p.Z);

        c = MathF.Cos(rotation.Y);
        s = MathF.Sin(rotation.Y);
        p = new Vector3(p.X * c - p.Z * s, p.Y, p.X * s + p.Z * c);

        c = MathF.Cos(rotation.X);
        s = MathF.Sin(rotation.X);
        p = new Vector3(p.X, p.Z * s + p.Y * c, p.Z * c - p.Y * s);

        return p + translation;
    }

    private void AddBox(Vector3 half)
    {
        int b = positions.Count;
        // Vertex i has bit 0 → +x, bit 1 → +y, bit 2 → +z.
        for (int i = 0; i < 8; i++)
            positions.Add(new Vector3(
                (i & 1) != 0 ? half.X : -half.X,
                (i & 2) != 0 ? half.Y : -half.Y,
                (i & 4) != 0 ? half.Z : -half.Z));

        ReadOnlySpan<int> faces =
        [
            0, 4, 6, 2, // -x
            1, 3, 7, 5, // +x
            0, 1, 5, 4, // -y
            2, 6, 7, 3, // +y
            0, 2, 3, 1, // -z
            4, 5, 7, 6, // +z
        ];
        for (int f = 0; f < faces.Length; f += 4)
        {
            triangles.Add(new Triangle(b + faces[f], b + faces[f + 1], b + faces[f + 2], Smooth: false));
            triangles.Add(new Triangle(b + faces[f], b + faces[f + 2], b + faces[f + 3], Smooth: false));
        }

        for (int i = 0; i < 8; i++)
            foreach (int bit in (ReadOnlySpan<int>)[1, 2, 4])
                if ((i & bit) == 0)
                    AddEdge(b + i, b + (i | bit));
    }

    private void AddTorus(TorusDef t)
    {
        float tubeStep = MathF.Tau / t.TubeSegments;
        float ringStep = MathF.Tau / t.RingSegments;
        AddGrid(t.TubeSegments, t.RingSegments, wrapRows: true, wrapColumns: true, (i, j) =>
        {
            float tube = (i + 0.5f) * tubeStep;
            float ring = j * ringStep;
            float r = t.RingRadius + t.TubeRadius * MathF.Cos(tube);
            return new Vector3(r * MathF.Cos(ring), r * MathF.Sin(ring), t.TubeRadius * MathF.Sin(tube));
        });
    }

    private enum PatchBasis { BSpline, Bezier }

    /// <summary>Both supported surfaces consist of 2×2 bicubic segments.</summary>
    private void AddPatch(Vector3[,] control, PatchBasis basis, int resolution)
    {
        const int segments = 2;
        int samples = segments * resolution + 1;
        AddGrid(samples, samples, wrapRows: false, wrapColumns: false,
            (i, j) => EvaluatePatch(control, basis, (float)i / resolution, (float)j / resolution));
    }

    /// <param name="s">Parameter along the first control index, 0..2.</param>
    /// <param name="t">Parameter along the second control index, 0..2.</param>
    private static Vector3 EvaluatePatch(Vector3[,] control, PatchBasis basis, float s, float t)
    {
        const int segments = 2;
        int stride = basis == PatchBasis.Bezier ? 3 : 1;
        int si = Math.Min((int)s, segments - 1);
        int ti = Math.Min((int)t, segments - 1);

        Span<float> bs = stackalloc float[4];
        Span<float> bt = stackalloc float[4];
        Basis(basis, s - si, bs);
        Basis(basis, t - ti, bt);

        var sum = Vector3.Zero;
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 4; c++)
                sum += bs[r] * bt[c] * control[si * stride + r, ti * stride + c];
        return sum;
    }

    private static void Basis(PatchBasis basis, float u, Span<float> w)
    {
        float v = 1 - u;
        if (basis == PatchBasis.Bezier)
        {
            w[0] = v * v * v;
            w[1] = 3 * u * v * v;
            w[2] = 3 * u * u * v;
            w[3] = u * u * u;
        }
        else
        {
            const float sixth = 1f / 6f;
            w[0] = v * v * v * sixth;
            w[1] = (3 * u * u * u - 6 * u * u + 4) * sixth;
            w[2] = (-3 * u * u * u + 3 * u * u + 3 * u + 1) * sixth;
            w[3] = u * u * u * sixth;
        }
    }

    /// <summary>Adds a rows×columns vertex grid, two smooth triangles per quad and the quad outlines as edges.</summary>
    private void AddGrid(int rows, int columns, bool wrapRows, bool wrapColumns, Func<int, int, Vector3> position)
    {
        int b = positions.Count;
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < columns; j++)
                positions.Add(position(i, j));

        int Index(int i, int j) => b + (i % rows) * columns + (j % columns);

        int quadRows = wrapRows ? rows : rows - 1;
        int quadColumns = wrapColumns ? columns : columns - 1;

        for (int i = 0; i < quadRows; i++)
        {
            for (int j = 0; j < quadColumns; j++)
            {
                int a = Index(i, j), bb = Index(i, j + 1), c = Index(i + 1, j), d = Index(i + 1, j + 1);
                triangles.Add(new Triangle(a, bb, c, Smooth: true));
                triangles.Add(new Triangle(d, c, bb, Smooth: true));
            }
        }

        for (int i = 0; i < rows; i++)
            for (int j = 0; j < quadColumns; j++)
                AddEdge(Index(i, j), Index(i, j + 1));
        for (int i = 0; i < quadRows; i++)
            for (int j = 0; j < columns; j++)
                AddEdge(Index(i, j), Index(i + 1, j));
    }

    private void AddEdge(int a, int b) => edges.Add(a < b ? new Edge(a, b) : new Edge(b, a));

    private Mesh ToMesh()
    {
        var pos = positions.ToArray();
        var normals = new Vector3[pos.Length];

        // Area-weighted average of the normals of adjacent smooth faces.
        foreach (var t in triangles)
        {
            if (!t.Smooth)
                continue;
            var n = Vector3.Cross(pos[t.B] - pos[t.A], pos[t.C] - pos[t.A]);
            normals[t.A] += n;
            normals[t.B] += n;
            normals[t.C] += n;
        }
        for (int i = 0; i < normals.Length; i++)
        {
            float length = normals[i].Length();
            normals[i] = length > 1e-12f ? normals[i] / length : Vector3.UnitZ;
        }

        return new Mesh(pos, normals, colors.ToArray(), triangles.ToArray(), edges.ToArray());
    }
}
