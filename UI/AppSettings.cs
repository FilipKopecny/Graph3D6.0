using System.Text.Json;
using Graph3D.Rendering;

namespace Graph3D.UI;

/// <summary>User preferences kept between runs in %LOCALAPPDATA%\Graph3D\settings.json.</summary>
internal sealed class AppSettings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Graph3D", "settings.json");

    /// <summary>Renderer key understood by <see cref="RendererCatalog.Create"/>.</summary>
    public string Renderer { get; set; } = RendererCatalog.Software;

    public static AppSettings Load()
    {
        try
        {
            return File.Exists(FilePath)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings()
                : new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppSettings(); // unreadable settings are not worth bothering the user about
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The choice still applies to this session.
        }
    }
}
