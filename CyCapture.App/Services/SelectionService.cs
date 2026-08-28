using CyCapture.Models;
using CyCapture.Platform.Windows;
using CyCapture.Views;

namespace CyCapture.Services;

internal sealed class SelectionService
{
    private readonly List<SelectionOverlayWindow> _windows = [];
    private TaskCompletionSource<CaptureSelection?>? _pending;

    internal bool IsSelecting => _pending is not null;

    internal async Task<CaptureSelection?> SelectAsync(CaptureSelectionMode selectionMode)
    {
        if (_pending is not null) return null;
        var monitors = NativeMethods.GetMonitors();
        if (monitors.Count == 0) throw new InvalidOperationException("Aucun écran Windows n’a été détecté.");

        var regions = selectionMode is CaptureSelectionMode.Smart or CaptureSelectionMode.Window
            ? await Task.Run(NativeMethods.EnumerateSelectableRegions)
            : [];
        var captures = await Task.Run(() => monitors
            .Select(monitor => (Monitor: monitor, Bitmap: ScreenCapture.Capture(monitor.Bounds)))
            .ToArray());

        _pending = new TaskCompletionSource<CaptureSelection?>(TaskCreationOptions.RunContinuationsAsynchronously);
        foreach (var capture in captures)
        {
            using var drawingBitmap = capture.Bitmap;
            var screenshot = ScreenCapture.ToAvaloniaBitmap(drawingBitmap);
            var window = new SelectionOverlayWindow(screenshot, capture.Monitor, regions, selectionMode);
            window.SelectionCompleted += Complete;
            window.Canceled += Cancel;
            window.Closed += (_, _) => screenshot.Dispose();
            _windows.Add(window);
            window.Show();
        }

        var result = await _pending.Task;
        CloseOverlays();
        _pending = null;
        NativeMethods.DwmFlush();
        await Task.Delay(60);
        return result;
    }

    internal void CancelSelection()
    {
        _pending?.TrySetResult(null);
    }

    private void Complete(object? sender, CaptureSelection selection) => _pending?.TrySetResult(selection);
    private void Cancel(object? sender, EventArgs args) => _pending?.TrySetResult(null);

    private void CloseOverlays()
    {
        foreach (var window in _windows)
        {
            window.SelectionCompleted -= Complete;
            window.Canceled -= Cancel;
            window.Hide();
            window.Close();
        }
        _windows.Clear();
    }
}
