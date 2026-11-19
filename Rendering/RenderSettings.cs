namespace Graph3D.Rendering;

public enum RenderMode
{
    /// <summary>Vertices only.</summary>
    Points,

    /// <summary>All edges, depth-cued.</summary>
    Wireframe,

    /// <summary>One colour per triangle (constant shading).</summary>
    Flat,

    /// <summary>Lighting computed per vertex and interpolated across the triangle.</summary>
    Gouraud,

    /// <summary>Normals interpolated across the triangle and lighting computed per pixel.</summary>
    Phong,
}

public sealed class RenderSettings
{
    public RenderMode Mode { get; set; } = RenderMode.Phong;

    /// <summary>Default surface colour (sRGB) for objects without a <c>farba</c>. The original program used pure red.</summary>
    public Color BaseColor { get; set; } = Color.FromArgb(225, 40, 40);

    public float Ambient { get; set; } = 0.05f;

    public float Diffuse { get; set; } = 0.95f;

    public float Specular { get; set; } = 0.5f;

    public float Shininess { get; set; } = 40f;
}
