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
        const int thickness = 2;
        const int gap = 1;

        if (bounds.Y - thickness - gap >= screen.Y)
            Add(new PixelBounds(bounds.X, bounds.Y - thickness - gap, bounds.Width, thickness), selection.Monitor.Scale, false);
        if (bounds.Bottom + thickness + gap <= screen.Bottom)
            Add(new PixelBounds(bounds.X, bounds.Bottom + gap, bounds.Width, thickness), selection.Monitor.Scale, false);
        if (bounds.X - thickness - gap >= screen.X)
            Add(new PixelBounds(bounds.X - thickness - gap, bounds.Y, thickness, bounds.Height), selection.Monitor.Scale, false);
        if (bounds.Right + thickness + gap <= screen.Right)
            Add(new PixelBounds(bounds.Right + gap, bounds.Y, thickness, bounds.Height), selection.Monitor.Scale, false);

        const int hudWidth = 144;
        const int hudHeight = 34;
        PixelBounds? hud = null;
        if (bounds.Y - hudHeight - 8 >= screen.Y)
            hud = new PixelBounds(Math.Clamp(bounds.X, screen.X, screen.Right - hudWidth), bounds.Y - hudHeight - 7, hudWidth, hudHeight);
        else if (bounds.Bottom + hudHeight + 8 <= screen.Bottom)
            hud = new PixelBounds(Math.Clamp(bounds.X, screen.X, screen.Right - hudWidth), bounds.Bottom + 7, hudWidth, hudHeight);
        if (hud is not null) Add(hud, selection.Monitor.Scale, true);
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
