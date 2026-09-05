using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CyCapture.Models;
using CyCapture.Platform.Windows;

namespace CyCapture.Views;

internal sealed class SelectionSurface : Control
{
    private static readonly IBrush ShadeBrush = new SolidColorBrush(Color.FromArgb(112, 0, 0, 0));
    private static readonly IPen BorderPen = new Pen(new SolidColorBrush(Color.FromRgb(155, 255, 40)), 2);
    private static readonly IPen CrosshairPen = new Pen(new SolidColorBrush(Color.FromArgb(155, 155, 255, 40)), 1);

    private readonly Bitmap _background;
    private readonly byte[]? _frozenFramePng;
    private readonly MonitorDescriptor _monitor;
    private readonly IReadOnlyList<SelectableRegion> _regions;
    private readonly IReadOnlyList<SelectableWindowLayer> _windowLayers;
    private readonly CaptureSelectionMode _selectionMode;
    private readonly bool _quickAnnotationsEnabled;
    private readonly QuickAnnotationSession _annotationSession;
    private Avalonia.Point? _dragStart;
    private Avalonia.Point _pointer = new(-1, -1);
    private SelectableRegion? _pressedCandidate;
    private PixelBounds? _highlight;
    private List<AnnotationPoint>? _workingPoints;
    private bool _lastAnnotationMode;

    internal SelectionSurface(
        Bitmap background,
        byte[]? frozenFramePng,
        MonitorDescriptor monitor,
        IReadOnlyList<SelectableRegion> regions,
        IReadOnlyList<SelectableWindowLayer> windowLayers,
        CaptureSelectionMode selectionMode,
        bool quickAnnotationsEnabled,
        QuickAnnotationSession annotationSession)
    {
        _background = background;
        _frozenFramePng = frozenFramePng;
        _monitor = monitor;
        _regions = regions;
        _windowLayers = windowLayers;
        _selectionMode = selectionMode;
        _quickAnnotationsEnabled = quickAnnotationsEnabled;
        _annotationSession = annotationSession;
        _lastAnnotationMode = annotationSession.AnnotationModeArmed;
        _annotationSession.Changed += AnnotationSessionChanged;
        Cursor = new Cursor(StandardCursorType.Cross);
        Focusable = true;
    }

    internal bool IsAnnotating => _annotationSession.AnnotationModeArmed;
    internal bool AnnotationModeArmed
    {
        get => _annotationSession.AnnotationModeArmed;
        set => _annotationSession.SetAnnotationMode(value);
    }
    internal QuickAnnotationKind AnnotationTool
    {
        get => _annotationSession.Tool;
        set => _annotationSession.SetTool(value);
    }
    internal string AnnotationColor
    {
        get => _annotationSession.Color;
        set => _annotationSession.SetColor(value);
    }
    internal string AnnotationText
    {
        get => _annotationSession.Text;
        set => _annotationSession.SetText(value);
    }
    internal int AnnotationCount => _annotationSession.AnnotationCount;

    internal event EventHandler<CaptureSelection>? SelectionCompleted;
    internal event EventHandler<(PixelBounds? Bounds, string Label)>? HighlightChanged;
    internal event EventHandler<string>? AnnotationHint;
    internal event EventHandler? AnnotationStateChanged;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var source = new Rect(0, 0, _background.PixelSize.Width, _background.PixelSize.Height);
        context.DrawImage(_background, source, Bounds);

        if (!AnnotationModeArmed)
        {
            context.DrawRectangle(ShadeBrush, null, Bounds);
            if (_highlight is { Width: > 0, Height: > 0 } highlight)
            {
                var local = ToLogicalLocal(highlight);
                using (context.PushClip(local))
                    context.DrawImage(_background, source, Bounds);
                context.DrawRectangle(null, BorderPen, local);
            }
        }

        foreach (var element in _annotationSession.Annotations) DrawAnnotation(context, element);
        if (_workingPoints is { Count: > 0 })
            DrawAnnotation(context, new QuickAnnotationElement(
                AnnotationTool,
                _workingPoints,
                AnnotationColor,
                AnnotationText));

        if (_pointer.X >= 0 && _pointer.Y >= 0)
        {
            context.DrawLine(CrosshairPen, new Avalonia.Point(0, _pointer.Y), new Avalonia.Point(Bounds.Width, _pointer.Y));
            context.DrawLine(CrosshairPen, new Avalonia.Point(_pointer.X, 0), new Avalonia.Point(_pointer.X, Bounds.Height));
        }
    }

    internal void UndoAnnotation()
    {
        _annotationSession.Undo();
    }

    internal void ClearAnnotations()
    {
        _annotationSession.Clear();
    }

    protected override void OnPointerMoved(PointerEventArgs args)
    {
        base.OnPointerMoved(args);
        _pointer = args.GetPosition(this);
        if (AnnotationModeArmed)
        {
            UpdateWorkingAnnotation(_pointer);
            InvalidateVisual();
            return;
        }

        if (_dragStart is { } start && _selectionMode is CaptureSelectionMode.Smart or CaptureSelectionMode.Region)
            UpdateHighlight(DragBounds(start, _pointer), "ZONE LIBRE");
        else
        {
            var candidate = CandidateAt(_pointer);
            if (candidate is null)
            {
                _highlight = null;
                HighlightChanged?.Invoke(this, (null, string.Empty));
            }
            else
                UpdateHighlight(candidate.Bounds, CandidateLabel(candidate));
        }
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs args)
    {
        base.OnPointerPressed(args);
        var point = args.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed) return;
        Focus();
        _pointer = point.Position;

        if (AnnotationModeArmed)
        {
            BeginAnnotation(args, point.Position);
            return;
        }

        _dragStart = point.Position;
        _pressedCandidate = CandidateAt(point.Position);
        args.Pointer.Capture(this);
        args.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs args)
    {
        base.OnPointerReleased(args);
        if (AnnotationModeArmed)
        {
            FinishAnnotation(args);
            return;
        }

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
            if (!AnnotationModeArmed)
            {
                _highlight = null;
                HighlightChanged?.Invoke(this, (null, string.Empty));
            }
            InvalidateVisual();
        }
    }

    private void BeginAnnotation(PointerPressedEventArgs args, Avalonia.Point position)
    {
        var globalPosition = ToGlobal(position);
        if (!_monitor.Bounds.Contains(globalPosition.X, globalPosition.Y))
        {
            AnnotationHint?.Invoke(this, "Placez l’annotation à l’intérieur de l’écran.");
            args.Handled = true;
            return;
        }

        if (AnnotationTool == QuickAnnotationKind.Text)
        {
            if (string.IsNullOrWhiteSpace(AnnotationText))
            {
                AnnotationHint?.Invoke(this, "Saisissez le texte dans la barre, puis cliquez dans l’image.");
                args.Handled = true;
                return;
            }
            _annotationSession.Add(new QuickAnnotationElement(
                QuickAnnotationKind.Text,
                [globalPosition],
                AnnotationColor,
                AnnotationText.Trim()));
            InvalidateVisual();
            args.Handled = true;
            return;
        }

        _dragStart = position;
        _workingPoints = [globalPosition];
        args.Pointer.Capture(this);
        args.Handled = true;
    }

    private void UpdateWorkingAnnotation(Avalonia.Point position)
    {
        if (_dragStart is null || _workingPoints is null) return;
        var global = ClampToSelection(ToGlobal(position), _monitor.Bounds);
        if (AnnotationTool == QuickAnnotationKind.Freehand)
        {
            var last = _workingPoints[^1];
            if (Math.Abs(last.X - global.X) + Math.Abs(last.Y - global.Y) >= 2)
                _workingPoints.Add(global);
        }
        else if (_workingPoints.Count == 1)
            _workingPoints.Add(global);
        else
            _workingPoints[^1] = global;
    }

    private void FinishAnnotation(PointerReleasedEventArgs args)
    {
        if (_dragStart is null || _workingPoints is null) return;
        UpdateWorkingAnnotation(args.GetPosition(this));
        args.Pointer.Capture(null);
        _dragStart = null;
        var minimumPoints = AnnotationTool == QuickAnnotationKind.Freehand ? 2 : 2;
        if (_workingPoints.Count >= minimumPoints)
        {
            var first = _workingPoints[0];
            var last = _workingPoints[^1];
            if (AnnotationTool == QuickAnnotationKind.Freehand || Math.Abs(first.X - last.X) + Math.Abs(first.Y - last.Y) >= 5)
            {
                _annotationSession.Add(new QuickAnnotationElement(
                    AnnotationTool,
                    _workingPoints.ToArray(),
                    AnnotationColor,
                    AnnotationText));
            }
        }
        _workingPoints = null;
        InvalidateVisual();
        args.Handled = true;
    }

    private void DrawAnnotation(DrawingContext context, QuickAnnotationElement element)
    {
        if (element.Points.Count == 0) return;
        var brush = new SolidColorBrush(ParseColor(element.Color));
        var pen = new Pen(brush, 3.2, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        var points = element.Points.Select(ToLogicalLocal).ToArray();

        switch (element.Kind)
        {
            case QuickAnnotationKind.Freehand when points.Length >= 2:
                for (var index = 1; index < points.Length; index++)
                    context.DrawLine(pen, points[index - 1], points[index]);
                break;
            case QuickAnnotationKind.Rectangle when points.Length >= 2:
                context.DrawRectangle(null, pen, Normalize(points[0], points[^1]));
                break;
            case QuickAnnotationKind.Arrow when points.Length >= 2:
                DrawArrow(context, pen, points[0], points[^1]);
                break;
            case QuickAnnotationKind.Text when !string.IsNullOrWhiteSpace(element.Text):
                var text = new FormattedText(
                    element.Text,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Inter", FontStyle.Normal, FontWeight.Bold),
                    21,
                    brush);
                context.DrawText(text, points[0]);
                break;
        }
    }

    private static void DrawArrow(DrawingContext context, IPen pen, Avalonia.Point start, Avalonia.Point end)
    {
        context.DrawLine(pen, start, end);
        var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
        const double size = 15;
        var left = new Avalonia.Point(
            end.X - size * Math.Cos(angle - Math.PI / 6),
            end.Y - size * Math.Sin(angle - Math.PI / 6));
        var right = new Avalonia.Point(
            end.X - size * Math.Cos(angle + Math.PI / 6),
            end.Y - size * Math.Sin(angle + Math.PI / 6));
        context.DrawLine(pen, end, left);
        context.DrawLine(pen, end, right);
    }

    private SelectableRegion? CandidateAt(Avalonia.Point point)
    {
        if (_selectionMode == CaptureSelectionMode.Screen)
            return new SelectableRegion(0, _monitor.DisplayName, SelectionKind.Screen, _monitor.Bounds);
        if (_selectionMode == CaptureSelectionMode.Region) return null;

        var globalX = _monitor.Bounds.X + (int)Math.Round(point.X * _monitor.Scale);
        var globalY = _monitor.Bounds.Y + (int)Math.Round(point.Y * _monitor.Scale);
        var frontWindow = _windowLayers
            .Where(item => item.Bounds.Contains(globalX, globalY))
            .MinBy(item => item.ZOrder);
        var childPath = frontWindow is null || _selectionMode == CaptureSelectionMode.Window
            ? (IReadOnlyList<nint>)[]
            : NativeMethods.ChildWindowPathAt(frontWindow.Handle, globalX, globalY);
        return SelectionCandidateResolver.Resolve(
                   _regions,
                   _windowLayers,
                   globalX,
                   globalY,
                   _selectionMode,
                   childPath)
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

    private Avalonia.Point ToLogicalLocal(AnnotationPoint global) => new(
        (global.X - _monitor.Bounds.X) / _monitor.Scale,
        (global.Y - _monitor.Bounds.Y) / _monitor.Scale);

    private AnnotationPoint ToGlobal(Avalonia.Point local) => new(
        _monitor.Bounds.X + (int)Math.Round(local.X * _monitor.Scale),
        _monitor.Bounds.Y + (int)Math.Round(local.Y * _monitor.Scale));

    private void UpdateHighlight(PixelBounds bounds, string label)
    {
        _highlight = bounds.Intersect(_monitor.Bounds);
        HighlightChanged?.Invoke(this, (_highlight, label));
    }

    private void Complete(PixelBounds bounds, SelectionKind kind, string title, nint handle)
    {
        var clipped = bounds.Intersect(_monitor.Bounds);
        if (clipped.Width <= 0 || clipped.Height <= 0) return;
        var selection = new CaptureSelection(clipped, _monitor, kind, title, handle);
        if (_quickAnnotationsEnabled)
            selection = selection with
            {
                FrozenFramePng = _frozenFramePng,
                QuickAnnotations = new QuickAnnotationDocument(_annotationSession.Annotations.ToArray())
            };
        SelectionCompleted?.Invoke(this, selection);
    }

    private void AnnotationSessionChanged(object? sender, EventArgs args)
    {
        if (_lastAnnotationMode != _annotationSession.AnnotationModeArmed)
        {
            _lastAnnotationMode = _annotationSession.AnnotationModeArmed;
            _dragStart = null;
            _workingPoints = null;
            _pressedCandidate = null;
            _highlight = null;
            HighlightChanged?.Invoke(this, (null, string.Empty));
        }
        AnnotationStateChanged?.Invoke(this, EventArgs.Empty);
        InvalidateVisual();
    }

    private static AnnotationPoint ClampToSelection(AnnotationPoint point, PixelBounds bounds) => new(
        Math.Clamp(point.X, bounds.X, Math.Max(bounds.X, bounds.Right - 1)),
        Math.Clamp(point.Y, bounds.Y, Math.Max(bounds.Y, bounds.Bottom - 1)));

    private static Rect Normalize(Avalonia.Point first, Avalonia.Point second) => new(
        Math.Min(first.X, second.X),
        Math.Min(first.Y, second.Y),
        Math.Abs(second.X - first.X),
        Math.Abs(second.Y - first.Y));

    private static Color ParseColor(string value)
    {
        try { return Color.Parse(value); }
        catch { return Color.FromRgb(255, 70, 87); }
    }

    private static string CandidateLabel(SelectableRegion? candidate) => candidate?.Kind switch
    {
        SelectionKind.Client => $"CONTENU · {candidate.Title}",
        SelectionKind.Control => $"CONTRÔLE · {candidate.Title}",
        SelectionKind.Window => $"FENÊTRE · {candidate.Title}",
        _ => $"ÉCRAN · {candidate?.Title ?? string.Empty}"
    };
}
