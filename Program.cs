using System.Drawing.Imaging;
using Graph3D.Geometry;
using Graph3D.Rendering;
using Graph3D.Scene;
using Graph3D.UI;

namespace Graph3D;

internal static class Program
{
    /// <summary>
    /// <c>Graph3D [scene.dat]</c> opens the viewer.
    /// <c>Graph3D --snapshot scene.dat out.png [mode] [width] [height] [renderer]</c> renders one image without a window;
    /// renderer is <c>cpu</c> (default), <c>d3d</c>, <c>d3d:N</c> (adapter N) or <c>opengl</c>.
    /// </summary>
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0].Equals("--snapshot", StringComparison.OrdinalIgnoreCase))
            return Snapshot(args);

        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.System);
        Application.Run(new MainForm(args.FirstOrDefault()));
        return 0;
    }

    private static int Snapshot(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("usage: Graph3D --snapshot scene.dat out.png [points|wireframe|flat|gouraud|phong] [width] [height] [cpu|d3d|d3d:N|opengl]");
            return 2;
        }

        try
        {
            var scene = DatParser.Load(args[1]);
            var mesh = MeshBuilder.Build(scene);
            var mode = args.Length > 3 ? Enum.Parse<RenderMode>(args[3], ignoreCase: true) : RenderMode.Phong;
            int width = args.Length > 4 ? int.Parse(args[4]) : 1000;
            int height = args.Length > 5 ? int.Parse(args[5]) : 800;

            var renderer = RendererCatalog.Create(args.Length > 6 ? args[6] : "cpu");
            try
            {
                using var frame = new FrameBuffer(width, height);
                var camera = new Camera();
                camera.Reset(mesh, scene.Camera, width / (float)height);
                renderer.Render(mesh, camera, frame, new RenderSettings { Mode = mode });
                frame.Bitmap.Save(args[2], ImageFormat.Png);
            }
            finally
            {
                (renderer as IDisposable)?.Dispose();
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}
