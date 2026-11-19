using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Numerics;
using Graph3D.Geometry;
using Graph3D.Rendering;
using Graph3D.Scene;

namespace Graph3D.UI;

/// <summary>
/// Control that renders a mesh with <see cref="Renderer"/>.
/// Left drag rotates, right/middle drag pans, the wheel zooms, double-click resets the view.
/// </summary>
public sealed class Viewport : Control
{
    private const float RotationPerPixel = 0.01f;

    private IRenderer renderer = new SoftwareRenderer();
    private FrameBuffer? buffer;
    private Mesh? mesh;
    private CameraDef? cameraDefinition;
    private Point lastMouse;
    private MouseButtons dragButton;

    public Viewport()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        BackColor = Color.FromArgb(24, 26, 30);
        ForeColor = Color.Gainsboro;
    }

    /// <summary>Raised after each frame with the time the renderer took.</summary>
    public event EventHandler<TimeSpan>? FrameRendered;

    /// <summary>Raised when a GPU renderer fails while drawing; the viewport has already switched back to the software renderer.</summary>
    public event EventHandler<Exception>? RendererFailed;

    /// <summary>The renderer currently drawing the scene.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IRenderer Renderer => renderer;

    /// <summary>Switches to another renderer and releases the previous one.</summary>
    public void UseRenderer(IRenderer next)
    {
        if (ReferenceEquals(next, renderer))
            return;
        (renderer as IDisposable)?.Dispose();
        renderer = next;
        Invalidate();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Camera Camera { get; } = new();

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RenderSettings Settings { get; } = new();

    public bool HasScene => mesh is not null;

    public void SetScene(Mesh newMesh, CameraDef? camera)
    {
        mesh = newMesh;
        cameraDefinition = camera;
        ResetView();
    }

    public void ResetView()
    {
        if (mesh is null)
            return;
        Camera.Reset(mesh, cameraDefinition, ClientSize.Width / (float)Math.Max(1, ClientSize.Height));
        Invalidate();
    }

    public void Rotate(float yaw, float pitch)
    {
        Camera.Yaw += yaw;
        Camera.Pitch += pitch;
        Invalidate();
    }

    /// <summary>Pans by a number of screen pixels.</summary>
    public void PanPixels(float dx, float dy)
    {
        float scale = Camera.WorldUnitsPerPixel(ClientSize.Height);
        Camera.Pan += new Vector2(dx * scale, dy * scale);
        Invalidate();
    }

    public void Zoom(float factor)
    {
        Camera.Distance *= factor;
        Invalidate();
    }

    /// <summary>Copy of the last rendered frame, or null when nothing has been rendered.</summary>
    public Bitmap? Snapshot() => buffer is null || mesh is null ? null : new Bitmap(buffer.Bitmap);

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Everything is drawn in OnPaint.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (mesh is null)
        {
            e.Graphics.Clear(BackColor);
            TextRenderer.DrawText(e.Graphics, "Open a .dat scene file (Ctrl+O) or drop one here.", Font,
                ClientRectangle, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        int width = Math.Max(1, ClientSize.Width), height = Math.Max(1, ClientSize.Height);
        if (buffer is null || buffer.Width != width || buffer.Height != height)
        {
            buffer?.Dispose();
            buffer = new FrameBuffer(width, height);
        }

        long start = Stopwatch.GetTimestamp();
        try
        {
            renderer.Render(mesh, Camera, buffer, Settings);
        }
        catch (Exception ex) when (renderer is not SoftwareRenderer)
        {
            // A lost or reset graphics device must not take the application down.
            UseRenderer(new SoftwareRenderer());
            renderer.Render(mesh, Camera, buffer, Settings);
            RendererFailed?.Invoke(this, ex);
        }
        var elapsed = Stopwatch.GetElapsedTime(start);

        e.Graphics.CompositingMode = CompositingMode.SourceCopy;
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.DrawImageUnscaled(buffer.Bitmap, 0, 0);

        FrameRendered?.Invoke(this, elapsed);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        lastMouse = e.Location;
        dragButton = e.Button;
        Capture = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (dragButton == MouseButtons.None || mesh is null)
            return;

        int dx = e.X - lastMouse.X, dy = e.Y - lastMouse.Y;
        lastMouse = e.Location;
        if (dx == 0 && dy == 0)
            return;

        if (dragButton == MouseButtons.Left)
            Rotate(dx * RotationPerPixel, -dy * RotationPerPixel);
        else
            PanPixels(dx, dy);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        dragButton = MouseButtons.None;
        Capture = false;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (mesh is not null)
            Zoom(MathF.Pow(0.88f, e.Delta / 120f));
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (e.Button == MouseButtons.Left)
            ResetView();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            buffer?.Dispose();
            (renderer as IDisposable)?.Dispose();
        }
        base.Dispose(disposing);
    }
}
