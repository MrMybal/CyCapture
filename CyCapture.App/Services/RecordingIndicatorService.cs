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
        foreach (var frameBounds in CalculateFrameBounds(bounds, screen))
            Add(frameBounds, selection.Monitor.Scale, false);

        const int hudWidth = 144;
        const int hudHeight = 34;
        PixelBounds? hud = null;
        if (bounds.Y - hudHeight - 8 >= screen.Y)
            hud = new PixelBounds(Math.Clamp(bounds.X, screen.X, screen.Right - hudWidth), bounds.Y - hudHeight - 7, hudWidth, hudHeight);
        else if (bounds.Bottom + hudHeight + 8 <= screen.Bottom)
            hud = new PixelBounds(Math.Clamp(bounds.X, screen.X, screen.Right - hudWidth), bounds.Bottom + 7, hudWidth, hudHeight);
        if (hud is not null) Add(hud, selection.Monitor.Scale, true);
    }

    private static IReadOnlyList<PixelBounds> CalculateFrameBounds(PixelBounds bounds, PixelBounds screen)
    {
        const int thickness = 1;
        var result = new List<PixelBounds>(4);
        if (bounds.Y - thickness >= screen.Y)
            result.Add(new PixelBounds(bounds.X, bounds.Y - thickness, bounds.Width, thickness));
        if (bounds.Bottom + thickness <= screen.Bottom)
            result.Add(new PixelBounds(bounds.X, bounds.Bottom, bounds.Width, thickness));
        if (bounds.X - thickness >= screen.X)
            result.Add(new PixelBounds(bounds.X - thickness, bounds.Y, thickness, bounds.Height));
        if (bounds.Right + thickness <= screen.Right)
            result.Add(new PixelBounds(bounds.Right, bounds.Y, thickness, bounds.Height));
        return result;
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
