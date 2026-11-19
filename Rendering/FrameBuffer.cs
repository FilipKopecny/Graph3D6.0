using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Graph3D.Rendering;

/// <summary>Colour buffer shared with a GDI+ <see cref="Bitmap"/> (no copying) plus a depth buffer.</summary>
public sealed class FrameBuffer : IDisposable
{
    private GCHandle handle;

    public FrameBuffer(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        Pixels = new int[Width * Height];
        Depth = new float[Width * Height];
        handle = GCHandle.Alloc(Pixels, GCHandleType.Pinned);
        Bitmap = new Bitmap(Width, Height, Width * 4, PixelFormat.Format32bppPArgb, handle.AddrOfPinnedObject());
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>Opaque 0xAARRGGBB pixels, row by row.</summary>
    public int[] Pixels { get; }

    /// <summary>1 / depth per pixel; 0 means nothing drawn (infinitely far).</summary>
    public float[] Depth { get; }

    public Bitmap Bitmap { get; }

    public void Dispose()
    {
        Bitmap.Dispose();
        if (handle.IsAllocated)
            handle.Free();
    }
}
