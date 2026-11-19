using System.Drawing.Imaging;
using Graph3D.Geometry;
using Graph3D.Rendering;
using Graph3D.Scene;

namespace Graph3D.UI;

public sealed class MainForm : Form
{
    private const string AppName = "Graph3D";
    private const float KeyRotation = 0.1f;   // radians per key press (same step as the original)
    private const float KeyPan = 25f;         // pixels per key press
    private const float KeyZoom = 0.9f;

    private const string ControlsHelp =
        "Mouse\n" +
        "  Left drag\t\tRotate\n" +
        "  Right / middle drag\tPan\n" +
        "  Wheel\t\tZoom\n" +
        "  Double-click\t\tReset view\n\n" +
        "Keyboard\n" +
        "  1 – 5\t\tPoints, Wireframe, Flat, Gouraud, Phong\n" +
        "  Arrow keys\t\tRotate\n" +
        "  W A S D\t\tPan\n" +
        "  F / G,  + / −\t\tZoom in / out\n" +
        "  R\t\tReset view\n" +
        "  Ctrl+O\t\tOpen scene\n" +
        "  Ctrl+S\t\tSave image\n\n" +
        "Renderer menu\n" +
        "  Software (CPU), Direct3D 11 on a chosen graphics card, or OpenGL.";

    private readonly Viewport viewport = new() { Dock = DockStyle.Fill };
    private readonly ToolStrip toolStrip = new() { GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(6, 3, 6, 3) };
    private readonly StatusStrip statusStrip = new() { SizingGrip = true };
    private readonly ToolStripStatusLabel fileLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel statsLabel = new();
    private readonly ToolStripStatusLabel frameLabel = new() { AutoSize = false, Width = 90, TextAlign = ContentAlignment.MiddleRight };
    private readonly Dictionary<RenderMode, ToolStripButton> modeButtons = [];
    private readonly ToolStripDropDownButton rendererButton = new()
    {
        ToolTipText = "What draws the picture: the CPU (our own rasterizer) or the graphics card",
    };
    private readonly AppSettings appSettings = AppSettings.Load();
    private readonly string? initialFile;

    public MainForm(string? initialFile)
    {
        this.initialFile = initialFile;

        Text = AppName;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1180, 820);
        MinimumSize = new Size(520, 360);
        StartPosition = FormStartPosition.CenterScreen;
        AllowDrop = true;

        BuildToolStrip();
        statusStrip.Items.AddRange([fileLabel, statsLabel, frameLabel]);

        // The fill control must be added first so it is docked last.
        Controls.Add(viewport);
        Controls.Add(toolStrip);
        Controls.Add(statusStrip);

        viewport.FrameRendered += (_, elapsed) => frameLabel.Text = $"{elapsed.TotalMilliseconds:0.0} ms";
        viewport.RendererFailed += (_, ex) => BeginInvoke(() =>
        {
            RememberRenderer(RendererCatalog.Software);
            ShowError($"The graphics card stopped drawing, so Graph3D switched to the software renderer.\n\n{ex.Message}");
        });
        SelectRenderer(appSettings.Renderer, showErrors: false);
        SetMode(viewport.Settings.Mode);
        UpdateStatus(null, null);

        Shown += (_, _) => LoadInitialScene();
        DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        };
        DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
                LoadScene(files[0]);
        };
    }

    private void BuildToolStrip()
    {
        toolStrip.Items.Add(new ToolStripButton("&Open…", null, (_, _) => OpenScene()) { ToolTipText = "Open a .dat scene (Ctrl+O)" });
        toolStrip.Items.Add(new ToolStripSeparator());

        (RenderMode Mode, string Text, string Tip)[] modes =
        [
            (RenderMode.Points, "Points", "Vertices only (1)"),
            (RenderMode.Wireframe, "Wireframe", "All edges (2)"),
            (RenderMode.Flat, "Flat", "Constant shading per triangle (3)"),
            (RenderMode.Gouraud, "Gouraud", "Per-vertex lighting, interpolated colours (4)"),
            (RenderMode.Phong, "Phong", "Interpolated normals, per-pixel lighting (5)"),
        ];
        foreach (var (mode, text, tip) in modes)
        {
            var button = new ToolStripButton(text, null, (_, _) => SetMode(mode)) { ToolTipText = tip };
            modeButtons[mode] = button;
            toolStrip.Items.Add(button);
        }

        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(new ToolStripButton("&Reset view", null, (_, _) => viewport.ResetView()) { ToolTipText = "Frame the whole scene (R)" });
        toolStrip.Items.Add(new ToolStripButton("&Colour…", null, (_, _) => PickColor()) { ToolTipText = "Default colour for objects without a 'farba' line" });
        toolStrip.Items.Add(new ToolStripButton("&Save image…", null, (_, _) => SaveImage()) { ToolTipText = "Save the current view as PNG (Ctrl+S)" });
        toolStrip.Items.Add(new ToolStripSeparator());
        BuildRendererMenu();
        toolStrip.Items.Add(rendererButton);

        toolStrip.Items.Add(new ToolStripButton("Help", null, (_, _) => ShowHelp())
        {
            Alignment = ToolStripItemAlignment.Right,
            ToolTipText = "Mouse and keyboard controls (F1)",
        });
    }

    private void BuildRendererMenu()
    {
        var items = rendererButton.DropDownItems;
        items.Add(RendererItem("Software (CPU)", RendererCatalog.Software,
            "Our own rasterizer, the way the original project drew everything"));
        items.Add(new ToolStripSeparator());

        var adapters = Direct3DRenderer.EnumerateAdapters();
        foreach (var adapter in adapters)
            items.Add(RendererItem($"Direct3D 11 – {adapter.Label}", RendererCatalog.Direct3DKey(adapter),
                "The graphics card fills the triangles; lighting runs in our own shaders"));
        if (adapters.Count == 0)
            items.Add(new ToolStripMenuItem("Direct3D 11 – no compatible graphics card found") { Enabled = false });

        items.Add(RendererItem("OpenGL 3.3 – graphics card chosen by Windows", RendererCatalog.OpenGL,
            "OpenGL cannot pick the graphics card: Windows and the driver decide (see the NVIDIA/AMD control panel)"));
    }

    private ToolStripMenuItem RendererItem(string text, string key, string tip) =>
        new(text, null, (_, _) => SelectRenderer(key, showErrors: true)) { Tag = key, ToolTipText = tip };

    /// <summary>Switches the viewport to the renderer with the given key; falls back to the CPU if it cannot start.</summary>
    private void SelectRenderer(string key, bool showErrors)
    {
        IRenderer renderer;
        try
        {
            renderer = RendererCatalog.Create(key);
        }
        catch (Exception ex) // drivers fail in many different ways; any of them means "use the CPU"
        {
            if (showErrors)
                ShowError($"Could not start this renderer, so Graph3D keeps using the software renderer.\n\n{ex.Message}");
            key = RendererCatalog.Software;
            renderer = new SoftwareRenderer();
        }

        viewport.UseRenderer(renderer);
        RememberRenderer(key);
    }

    private void RememberRenderer(string key)
    {
        appSettings.Renderer = key;
        appSettings.Save();
        foreach (var item in rendererButton.DropDownItems.OfType<ToolStripMenuItem>())
            item.Checked = key.Equals(item.Tag as string, StringComparison.OrdinalIgnoreCase);
        rendererButton.Text = $"Renderer: {viewport.Renderer.Name}";
    }

    private void LoadInitialScene()
    {
        string path = initialFile ?? Path.Combine(AppContext.BaseDirectory, "TOR.DAT");
        if (File.Exists(path))
            LoadScene(path);
        else if (initialFile is not null)
            ShowError($"File not found:\n{initialFile}");
    }

    private void LoadScene(string path)
    {
        try
        {
            var scene = DatParser.Load(path);
            var mesh = MeshBuilder.Build(scene);
            viewport.SetScene(mesh, scene.Camera);
            Text = $"{Path.GetFileName(path)} – {AppName}";
            UpdateStatus(path, mesh);
            if (scene.Primitives.Count == 0)
                ShowError($"{Path.GetFileName(path)} contains no objects.", MessageBoxIcon.Information);
        }
        catch (Exception ex) when (ex is DatFormatException or IOException or UnauthorizedAccessException)
        {
            ShowError($"Could not load the scene.\n\n{ex.Message}");
        }
    }

    private void UpdateStatus(string? path, Mesh? mesh)
    {
        fileLabel.Text = path ?? "No scene loaded";
        fileLabel.ToolTipText = path;
        statsLabel.Text = mesh is null
            ? string.Empty
            : $"{mesh.Positions.Length:N0} vertices · {mesh.Edges.Length:N0} edges · {mesh.Triangles.Length:N0} triangles";
    }

    private void SetMode(RenderMode mode)
    {
        viewport.Settings.Mode = mode;
        foreach (var (m, button) in modeButtons)
            button.Checked = m == mode;
        viewport.Invalidate();
    }

    private void OpenScene()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open scene",
            Filter = "Scene files (*.dat)|*.dat|All files (*.*)|*.*",
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            LoadScene(dialog.FileName);
    }

    private void PickColor()
    {
        using var dialog = new ColorDialog { Color = viewport.Settings.BaseColor, FullOpen = true };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        viewport.Settings.BaseColor = dialog.Color;
        viewport.Invalidate();
    }

    private void SaveImage()
    {
        using var image = viewport.Snapshot();
        if (image is null)
            return;

        using var dialog = new SaveFileDialog
        {
            Title = "Save image",
            Filter = "PNG image (*.png)|*.png",
            FileName = $"graph3d-{viewport.Settings.Mode.ToString().ToLowerInvariant()}.png",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            image.Save(dialog.FileName, ImageFormat.Png);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Runtime.InteropServices.ExternalException)
        {
            ShowError($"Could not save the image.\n\n{ex.Message}");
        }
    }

    private void ShowHelp() =>
        MessageBox.Show(this, ControlsHelp, $"{AppName} controls", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void ShowError(string message, MessageBoxIcon icon = MessageBoxIcon.Warning) =>
        MessageBox.Show(this, message, AppName, MessageBoxButtons.OK, icon);

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.O: OpenScene(); return true;
            case Keys.Control | Keys.S: SaveImage(); return true;
            case Keys.F1: ShowHelp(); return true;

            case Keys.D1 or Keys.NumPad1: SetMode(RenderMode.Points); return true;
            case Keys.D2 or Keys.NumPad2: SetMode(RenderMode.Wireframe); return true;
            case Keys.D3 or Keys.NumPad3: SetMode(RenderMode.Flat); return true;
            case Keys.D4 or Keys.NumPad4: SetMode(RenderMode.Gouraud); return true;
            case Keys.D5 or Keys.NumPad5: SetMode(RenderMode.Phong); return true;
        }

        if (!viewport.HasScene)
            return base.ProcessCmdKey(ref msg, keyData);

        switch (keyData)
        {
            case Keys.Left: viewport.Rotate(-KeyRotation, 0); return true;
            case Keys.Right: viewport.Rotate(KeyRotation, 0); return true;
            case Keys.Up: viewport.Rotate(0, KeyRotation); return true;
            case Keys.Down: viewport.Rotate(0, -KeyRotation); return true;

            case Keys.A: viewport.PanPixels(-KeyPan, 0); return true;
            case Keys.D: viewport.PanPixels(KeyPan, 0); return true;
            case Keys.W: viewport.PanPixels(0, -KeyPan); return true;
            case Keys.S: viewport.PanPixels(0, KeyPan); return true;

            case Keys.F or Keys.Oemplus or Keys.Add: viewport.Zoom(KeyZoom); return true;
            case Keys.G or Keys.OemMinus or Keys.Subtract: viewport.Zoom(1 / KeyZoom); return true;

            case Keys.R: viewport.ResetView(); return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
