using CyCapture.Models;
using CyCapture.Views;

namespace CyCapture.Services;

internal sealed class RecordingIndicatorService
{
    private readonly List<RecordingIndicatorWindow> _windows = [];

    internal void Show(CaptureSelection selection)
    {
        Hide();
        var bounds = selection.Bounds;
        var screen = selection.Monitor.Bounds;
        Add(CalculateOutlineBounds(bounds, screen), selection.Monitor.Scale, false);

        const int hudWidth = 144;
        const int hudHeight = 34;
        PixelBounds? hud = null;
        if (bounds.Y - hudHeight - 8 >= screen.Y)
            hud = new PixelBounds(Math.Clamp(bounds.X, screen.X, screen.Right - hudWidth), bounds.Y - hudHeight - 7, hudWidth, hudHeight);
        else if (bounds.Bottom + hudHeight + 8 <= screen.Bottom)
            hud = new PixelBounds(Math.Clamp(bounds.X, screen.X, screen.Right - hudWidth), bounds.Bottom + 7, hudWidth, hudHeight);
        hud ??= new PixelBounds(
            Math.Clamp(bounds.X + 8, screen.X, screen.Right - hudWidth),
            Math.Clamp(bounds.Y + 8, screen.Y, screen.Bottom - hudHeight),
            hudWidth,
            hudHeight);
        Add(hud, selection.Monitor.Scale, true);
    }

    private static PixelBounds CalculateOutlineBounds(PixelBounds bounds, PixelBounds screen)
    {
        const int outsideMargin = 2;
        var left = Math.Max(screen.X, bounds.X - outsideMargin);
        var top = Math.Max(screen.Y, bounds.Y - outsideMargin);
        var right = Math.Min(screen.Right, bounds.Right + outsideMargin);
        var bottom = Math.Min(screen.Bottom, bounds.Bottom + outsideMargin);
        return new PixelBounds(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
    }

    internal void UpdateElapsed(TimeSpan elapsed)
    {
        foreach (var window in _windows) window.UpdateElapsed(elapsed);
    }

    internal void Hide()
    {
        foreach (var window in _windows)
        {
            window.Hide();
            window.Close();
        }
        _windows.Clear();
    }

    private void Add(PixelBounds bounds, double scale, bool hud)
    {
        var window = new RecordingIndicatorWindow(bounds, scale, hud);
        _windows.Add(window);
        window.Show();
    }
}
