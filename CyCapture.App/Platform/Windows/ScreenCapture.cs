using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using CyCapture.Models;

namespace CyCapture.Platform.Windows;

internal static class ScreenCapture
{
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(nint value);

    internal static Bitmap Capture(PixelBounds bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) throw new ArgumentOutOfRangeException(nameof(bounds));
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    internal static Avalonia.Media.Imaging.Bitmap ToAvaloniaBitmap(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;
        return new Avalonia.Media.Imaging.Bitmap(stream);
    }

    internal static byte[] ToPngBytes(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    internal static Bitmap CropFrozenFrame(byte[] png, PixelBounds localBounds)
    {
        using var stream = new MemoryStream(png, writable: false);
        using var fullFrame = new Bitmap(stream);
        var bounds = new Rectangle(localBounds.X, localBounds.Y, localBounds.Width, localBounds.Height);
        return fullFrame.Clone(bounds, PixelFormat.Format32bppArgb);
    }

    internal static void CopyToClipboard(Bitmap bitmap)
    {
        Exception? lastError = null;
        for (var attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                var handle = bitmap.GetHbitmap();
                try
                {
                    var source = Imaging.CreateBitmapSourceFromHBitmap(
                        handle,
                        0,
                        System.Windows.Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                    source.Freeze();
                    System.Windows.Clipboard.SetImage(source);
                }
                finally
                {
                    DeleteObject(handle);
                }
                return;
            }
            catch (Exception error)
            {
                lastError = error;
                Thread.Sleep(35 * (attempt + 1));
            }
        }
        throw new InvalidOperationException("Le presse-papiers Windows est occupé.", lastError);
    }
}
