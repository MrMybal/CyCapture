using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CyCapture.Models;

namespace CyCapture.Views;

internal sealed class SelectionSurface : Control
{
    private static readonly IBrush ShadeBrush = new SolidColorBrush(Color.FromArgb(112, 0, 0, 0));
    private static readonly IPen BorderPen = new Pen(new SolidColorBrush(Color.FromRgb(155, 255, 40)), 2);
    private static readonly IPen CrosshairPen = new Pen(new SolidColorBrush(Color.FromArgb(155, 155, 255, 40)), 1);

    private readonly Bitmap _background;
    private readonly MonitorDescriptor _monitor;
    private readonly IReadOnlyList<SelectableRegion> _regions;
    private readonly CaptureSelectionMode _selectionMode;
    private Avalonia.Point? _dragStart;
    private Avalonia.Point _pointer;
    private SelectableRegion? _pressedCandidate;
    private PixelBounds? _highlight;
    private string _highlightLabel = string.Empty;

    internal SelectionSurface(
        Bitmap background,
        MonitorDescriptor monitor,
        IReadOnlyList<SelectableRegion> regions,
        CaptureSelectionMode selectionMode)
    {
        _background = background;
        _monitor = monitor;
        _regions = regions;
        _selectionMode = selectionMode;
        Cursor = new Cursor(StandardCursorType.Cross);
        Focusable = true;
    }

    internal event EventHandler<CaptureSelection>? SelectionCompleted;
    internal event EventHandler<(PixelBounds? Bounds, string Label)>? HighlightChanged;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var source = new Rect(0, 0, _background.PixelSize.Width, _background.PixelSize.Height);
        context.DrawImage(_background, source, Bounds);
        context.DrawRectangle(ShadeBrush, null, Bounds);

        if (_highlight is { Width: > 0, Height: > 0 } highlight)
        {
            var local = ToLogicalLocal(highlight);
            using (context.PushClip(local))
                context.DrawImage(_background, source, Bounds);
            context.DrawRectangle(null, BorderPen, local);
        }

        if (_pointer.X >= 0 && _pointer.Y >= 0)
        {
            context.DrawLine(CrosshairPen, new Avalonia.Point(0, _pointer.Y), new Avalonia.Point(Bounds.Width, _pointer.Y));
            context.DrawLine(CrosshairPen, new Avalonia.Point(_pointer.X, 0), new Avalonia.Point(_pointer.X, Bounds.Height));
        }
    }

    protected override void OnPointerMoved(PointerEventArgs args)
    {
        base.OnPointerMoved(args);
        _pointer = args.GetPosition(this);
        if (_dragStart is { } start && _selectionMode is CaptureSelectionMode.Smart or CaptureSelectionMode.Region)
        {
            UpdateHighlight(DragBounds(start, _pointer), "ZONE LIBRE");
        }
        else
        {
            var candidate = CandidateAt(_pointer);
            if (candidate is null)
            {
                _highlight = null;
                HighlightChanged?.Invoke(this, (null, string.Empty));
            }
            else
            {
                UpdateHighlight(candidate.Bounds, CandidateLabel(candidate));
            }
        }
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs args)
    {
        base.OnPointerPressed(args);
        var point = args.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed) return;
        Focus();
        _dragStart = point.Position;
        _pointer = point.Position;
        _pressedCandidate = CandidateAt(point.Position);
        args.Pointer.Capture(this);
        args.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs args)
    {
        base.OnPointerReleased(args);
        if (_dragStart is not { } start) return;
        var end = args.GetPosition(this);
        args.Pointer.Capture(null);
        _dragStart = null;
        var distance = Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
        if (_selectionMode == CaptureSelectionMode.Screen && _pressedCandidate is { } screen)
            Complete(screen.Bounds, screen.Kind, screen.Title, screen.Handle);
        else if (_selectionMode == CaptureSelectionMode.Window && distance < 7 && _pressedCandidate is { } window)
            Complete(window.Bounds, window.Kind, window.Title, window.Handle);
        else if (_selectionMode == CaptureSelectionMode.Smart && distance < 7 && _pressedCandidate is { } candidate)
            Complete(candidate.Bounds, candidate.Kind, candidate.Title, candidate.Handle);
        else if (_selectionMode is CaptureSelectionMode.Smart or CaptureSelectionMode.Region)
        {
            var bounds = DragBounds(start, end);
            if (bounds.Width >= 8 && bounds.Height >= 8)
                Complete(bounds, SelectionKind.Region, "Zone libre", 0);
        }
        _pressedCandidate = null;
        args.Handled = true;
    }

    protected override void OnPointerExited(PointerEventArgs args)
    {
        base.OnPointerExited(args);
        if (_dragStart is null)
        {
            _pointer = new Avalonia.Point(-1, -1);
            _highlight = null;
            HighlightChanged?.Invoke(this, (null, string.Empty));
            InvalidateVisual();
        }
    }

    private SelectableRegion? CandidateAt(Avalonia.Point point)
    {
        if (_selectionMode == CaptureSelectionMode.Screen)
            return new SelectableRegion(0, _monitor.DisplayName, SelectionKind.Screen, _monitor.Bounds);
        if (_selectionMode == CaptureSelectionMode.Region) return null;

        var globalX = _monitor.Bounds.X + (int)Math.Round(point.X * _monitor.Scale);
        var globalY = _monitor.Bounds.Y + (int)Math.Round(point.Y * _monitor.Scale);
        var matches = _regions
            .Where(item => item.Bounds.Contains(globalX, globalY))
            .Where(item => _selectionMode != CaptureSelectionMode.Window || item.Kind == SelectionKind.Window)
            .OrderBy(item => item.Bounds.Area)
            .ThenByDescending(item => item.Priority)
            .ToList();
        return matches.FirstOrDefault()
               ?? (_selectionMode == CaptureSelectionMode.Smart
                   ? new SelectableRegion(0, _monitor.DisplayName, SelectionKind.Screen, _monitor.Bounds)
                   : null);
    }

    private PixelBounds DragBounds(Avalonia.Point first, Avalonia.Point second)
    {
        var x1 = _monitor.Bounds.X + (int)Math.Round(first.X * _monitor.Scale);
        var y1 = _monitor.Bounds.Y + (int)Math.Round(first.Y * _monitor.Scale);
        var x2 = _monitor.Bounds.X + (int)Math.Round(second.X * _monitor.Scale);
        var y2 = _monitor.Bounds.Y + (int)Math.Round(second.Y * _monitor.Scale);
        return new PixelBounds(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x2 - x1), Math.Abs(y2 - y1))
            .Intersect(_monitor.Bounds);
    }

    private Rect ToLogicalLocal(PixelBounds global) => new(
        (global.X - _monitor.Bounds.X) / _monitor.Scale,
        (global.Y - _monitor.Bounds.Y) / _monitor.Scale,
        global.Width / _monitor.Scale,
        global.Height / _monitor.Scale);

    private void UpdateHighlight(PixelBounds bounds, string label)
    {
        _highlight = bounds.Intersect(_monitor.Bounds);
        _highlightLabel = label;
        HighlightChanged?.Invoke(this, (_highlight, label));
    }

    private void Complete(PixelBounds bounds, SelectionKind kind, string title, nint handle)
    {
        var clipped = bounds.Intersect(_monitor.Bounds);
        if (clipped.Width <= 0 || clipped.Height <= 0) return;
        SelectionCompleted?.Invoke(this, new CaptureSelection(clipped, _monitor, kind, title, handle));
    }

    private static string CandidateLabel(SelectableRegion? candidate) => candidate?.Kind switch
    {
        SelectionKind.Client => $"CONTENU · {candidate.Title}",
        SelectionKind.Control => $"CONTRÔLE · {candidate.Title}",
        SelectionKind.Window => $"FENÊTRE · {candidate.Title}",
        _ => $"ÉCRAN · {candidate?.Title ?? string.Empty}"
    };
}
