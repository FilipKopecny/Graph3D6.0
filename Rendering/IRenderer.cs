using System.Numerics;
using Graph3D.Geometry;

namespace Graph3D.Rendering;

/// <summary>Draws a mesh into a <see cref="FrameBuffer"/>.</summary>
public interface IRenderer
{
    /// <summary>Name shown in the user interface.</summary>
    string Name { get; }

    void Render(Mesh mesh, Camera camera, FrameBuffer target, RenderSettings settings);
}

/// <summary>Values both renderers share, so the CPU and GPU produce the same picture.</summary>
internal static class RenderConstants
{
    /// <summary>Closest distance in front of the eye that is still drawn.</summary>
    public const float NearPlane = 0.01f;

    /// <summary>Diameter of a vertex in points mode, in pixels.</summary>
    public const float PointSize = 3.5f;

    /// <summary>Line width in wireframe mode, in pixels.</summary>
    public const float LineWidth = 1.2f;

    public static readonly Color BackgroundTop = Color.FromArgb(46, 50, 58);

    public static readonly Color BackgroundBottom = Color.FromArgb(16, 17, 20);

    /// <summary>Direction towards the light in view space: from the viewer, slightly upper-left (+Y is down).</summary>
    public static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(-0.4f, -0.55f, 1f));
}
