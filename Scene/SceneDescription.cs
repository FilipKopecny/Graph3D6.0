using System.Numerics;

namespace Graph3D.Scene;

/// <summary>Camera line of a .dat file: <c>kamera x y z angle</c>.</summary>
/// <param name="PanX">Horizontal offset of the view (legacy <c>xk</c>).</param>
/// <param name="PanY">Vertical offset of the view (legacy <c>yk</c>).</param>
/// <param name="Distance">Legacy zoom distance (<c>zk</c>). The viewer frames the scene automatically, so this is informational.</param>
/// <param name="FieldOfView">Vertical field of view in radians (legacy <c>uhol</c>).</param>
public sealed record CameraDef(float PanX, float PanY, float Distance, float FieldOfView);

/// <summary>Base for every object in a scene. Rotation angles are in radians and are applied Z, then Y, then X (as in the original program).</summary>
public abstract record Primitive(Vector3 Position, Vector3 Rotation)
{
    /// <summary>Surface colour set by the last <c>farba</c> statement; <see cref="Color.Empty"/> means the viewer's default colour.</summary>
    public Color Color { get; init; } = Color.Empty;
}

/// <summary><c>kvader x y z dx dy dz xangle yangle zangle</c></summary>
public sealed record BoxDef(Vector3 Position, Vector3 Rotation, Vector3 Size) : Primitive(Position, Rotation);

/// <summary><c>toroid x y z rin rout xdetail ydetail xangle yangle zangle</c></summary>
/// <param name="TubeRadius">Radius of the tube (legacy <c>rin</c>).</param>
/// <param name="RingRadius">Distance from the torus centre to the tube centre (legacy <c>rout</c>).</param>
/// <param name="TubeSegments">Subdivisions around the tube (legacy <c>xjemnost</c>).</param>
/// <param name="RingSegments">Subdivisions around the ring (legacy <c>yjemnost</c>).</param>
public sealed record TorusDef(Vector3 Position, Vector3 Rotation, float TubeRadius, float RingRadius, int TubeSegments, int RingSegments)
    : Primitive(Position, Rotation);

/// <summary><c>meshbs x y z xangle yangle zangle</c> followed by 5×5 control points: a uniform bicubic B-spline surface.</summary>
public sealed record BSplinePatchDef(Vector3 Position, Vector3 Rotation, Vector3[,] ControlPoints) : Primitive(Position, Rotation)
{
    public const int GridSize = 5;
}

/// <summary><c>meshbe x y z xangle yangle zangle</c> followed by 7×7 control points: 2×2 joined bicubic Bézier patches.</summary>
public sealed record BezierPatchDef(Vector3 Position, Vector3 Rotation, Vector3[,] ControlPoints) : Primitive(Position, Rotation)
{
    public const int GridSize = 7;
}

/// <summary>Everything read from one .dat file.</summary>
public sealed record SceneDescription(CameraDef? Camera, IReadOnlyList<Primitive> Primitives);
