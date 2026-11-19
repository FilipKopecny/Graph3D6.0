using System.Numerics;
using Graph3D.Geometry;
using Graph3D.Scene;

namespace Graph3D.Rendering;

/// <summary>
/// Orbit camera. View space keeps the original program's screen convention:
/// +X is right, +Y is down and the viewer sits on the +Z axis looking towards −Z.
/// </summary>
public sealed class Camera
{
    public const float DefaultYaw = 0.45f;
    public const float DefaultPitch = -0.35f;
    public const float MinDistance = 0.05f;

    /// <summary>Point the camera orbits around.</summary>
    public Vector3 Target { get; set; }

    /// <summary>Rotation about the vertical axis, radians.</summary>
    public float Yaw { get; set; } = DefaultYaw;

    /// <summary>Rotation about the horizontal axis, radians.</summary>
    public float Pitch { get; set; } = DefaultPitch;

    /// <summary>Distance from the eye to <see cref="Target"/>.</summary>
    public float Distance
    {
        get;
        set => field = MathF.Max(MinDistance, value);
    } = 10f;

    /// <summary>Vertical field of view in radians.</summary>
    public float FieldOfView { get; set; } = 0.7f;

    /// <summary>Screen-aligned shift of the scene in world units.</summary>
    public Vector2 Pan { get; set; }

    public Matrix4x4 Rotation => Matrix4x4.CreateRotationY(Yaw) * Matrix4x4.CreateRotationX(Pitch);

    public Vector3 ToView(Vector3 world, in Matrix4x4 rotation) =>
        Vector3.Transform(world - Target, rotation) + new Vector3(Pan, 0);

    /// <summary>Size of one screen pixel, in world units, at the depth of <see cref="Target"/>.</summary>
    public float WorldUnitsPerPixel(int viewportHeight) =>
        2f * Distance * MathF.Tan(FieldOfView * 0.5f) / Math.Max(1, viewportHeight);

    /// <summary>
    /// Restores the initial view: centres the mesh on screen, moves the camera just far enough back
    /// for every vertex to be visible and then applies the scene's <c>kamera</c> settings.
    /// </summary>
    public void Reset(Mesh mesh, CameraDef? definition, float aspect)
    {
        const float margin = 1.06f;

        FieldOfView = definition is { FieldOfView: > 0.1f and < 2.6f } ? definition.FieldOfView : 0.7f;
        Yaw = DefaultYaw;
        Pitch = DefaultPitch;
        Target = mesh.BoundsCenter;
        Pan = Vector2.Zero;
        Distance = MathF.Max(mesh.BoundsRadius, 1f);
        if (mesh.Positions.Length == 0)
            return;

        var rotation = Rotation;

        // Centre the view-space bounding rectangle.
        var min = new Vector2(float.MaxValue);
        var max = new Vector2(float.MinValue);
        foreach (var p in mesh.Positions)
        {
            var v = ToView(p, rotation);
            min = Vector2.Min(min, new Vector2(v.X, v.Y));
            max = Vector2.Max(max, new Vector2(v.X, v.Y));
        }
        Pan = -(min + max) * 0.5f;

        // Perspective makes near parts look bigger, so alternate between fitting the distance and
        // re-centring on the projected image; a few rounds are enough to converge.
        float tanVertical = MathF.Tan(FieldOfView * 0.5f) / margin;
        float tanHorizontal = tanVertical * MathF.Max(0.1f, aspect);
        for (int round = 0; round < 4; round++)
        {
            float distance = MinDistance;
            foreach (var p in mesh.Positions)
            {
                var v = ToView(p, rotation);
                distance = MathF.Max(distance, v.Z + MathF.Max(MathF.Abs(v.X) / tanHorizontal, MathF.Abs(v.Y) / tanVertical));
            }
            Distance = distance;
            if (round == 3)
                break;

            min = new Vector2(float.MaxValue);
            max = new Vector2(float.MinValue);
            foreach (var p in mesh.Positions)
            {
                var v = ToView(p, rotation);
                var projected = new Vector2(v.X, v.Y) / (Distance - v.Z);
                min = Vector2.Min(min, projected);
                max = Vector2.Max(max, projected);
            }
            Pan -= (min + max) * 0.5f * Distance;
        }

        if (definition is not null)
            Pan += new Vector2(definition.PanX, definition.PanY);
    }
}
