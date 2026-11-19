namespace Graph3D.Rendering;

/// <summary>Creates renderers from short names, as used on the command line and in the saved settings.</summary>
public static class RendererCatalog
{
    public const string Software = "cpu";
    public const string Direct3D = "d3d";
    public const string OpenGL = "opengl";

    /// <summary>Settings key for Direct3D on a specific adapter.</summary>
    public static string Direct3DKey(GpuAdapterInfo adapter) => $"{Direct3D}:{adapter.Name}";

    /// <param name="key">
    /// <c>cpu</c>; <c>d3d</c> (the GPU with the most dedicated memory); <c>d3d:N</c> (adapter number N);
    /// <c>d3d:Name</c> (adapter by name); or <c>opengl</c>.
    /// </param>
    public static IRenderer Create(string key)
    {
        if (key.Equals(Software, StringComparison.OrdinalIgnoreCase))
            return new SoftwareRenderer();

        if (key.Equals(OpenGL, StringComparison.OrdinalIgnoreCase))
            return OpenGLRenderer.Create();

        if (key.Equals(Direct3D, StringComparison.OrdinalIgnoreCase) || key.StartsWith(Direct3D + ":", StringComparison.OrdinalIgnoreCase))
        {
            var adapters = Direct3DRenderer.EnumerateAdapters();
            if (adapters.Count == 0)
                throw new InvalidOperationException("No graphics card with Direct3D 11 support was found.");

            string selector = key.Length > Direct3D.Length + 1 ? key[(Direct3D.Length + 1)..] : string.Empty;
            var adapter = selector.Length == 0
                ? adapters.MaxBy(a => a.DedicatedMemory)!
                : int.TryParse(selector, out int index)
                    ? adapters.FirstOrDefault(a => a.Index == index)
                    : adapters.FirstOrDefault(a => a.Name.Equals(selector, StringComparison.OrdinalIgnoreCase));

            return adapter is null
                ? throw new InvalidOperationException($"Graphics card '{selector}' was not found.")
                : Direct3DRenderer.Create(adapter);
        }

        throw new ArgumentException($"Unknown renderer '{key}'. Use cpu, d3d, d3d:N or opengl.", nameof(key));
    }
}
