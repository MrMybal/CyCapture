namespace CyCapture.Models;

public enum CaptureMode
{
    Image,
    Video,
    Gif,
    Audio
}

public enum CaptureSelectionMode
{
    Smart,
    Window,
    Screen,
    Region
}

public enum SelectionKind
{
    Screen,
    Window,
    Client,
    Control,
    Region
}

public enum QuickAnnotationKind
{
    Freehand,
    Rectangle,
    Arrow,
    Text
}

public sealed record AnnotationPoint(int X, int Y);

public sealed record QuickAnnotationElement(
    QuickAnnotationKind Kind,
    IReadOnlyList<AnnotationPoint> Points,
    string Color,
    string Text = "");

public sealed record QuickAnnotationDocument(IReadOnlyList<QuickAnnotationElement> Elements);

public sealed record PixelBounds(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public long Area => (long)Width * Height;

    public bool Contains(int x, int y) => x >= X && y >= Y && x < Right && y < Bottom;

    public PixelBounds Intersect(PixelBounds other)
    {
        var left = Math.Max(X, other.X);
        var top = Math.Max(Y, other.Y);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);
        return right <= left || bottom <= top
            ? new PixelBounds(0, 0, 0, 0)
            : new PixelBounds(left, top, right - left, bottom - top);
    }
}

public sealed record MonitorDescriptor(
    nint Handle,
    string DeviceName,
    string DisplayName,
    PixelBounds Bounds,
    PixelBounds WorkArea,
    double Scale,
    bool IsPrimary);

public sealed record SelectableRegion(
    nint Handle,
    string Title,
    SelectionKind Kind,
    PixelBounds Bounds,
    int Priority = 0,
    nint RootWindowHandle = 0);

public sealed record SelectableWindowLayer(
    nint Handle,
    PixelBounds Bounds,
    int ZOrder);

public sealed record CaptureSelection(
    PixelBounds Bounds,
    MonitorDescriptor Monitor,
    SelectionKind Kind,
    string Title,
    nint WindowHandle = 0,
    byte[]? FrozenFramePng = null,
    QuickAnnotationDocument? QuickAnnotations = null)
{
    public PixelBounds LocalBounds => new(
        Bounds.X - Monitor.Bounds.X,
        Bounds.Y - Monitor.Bounds.Y,
        Bounds.Width,
        Bounds.Height);
}

public sealed record CaptureArtifact(
    string Path,
    CaptureMode Mode,
    CaptureSelection Selection,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt);

public sealed record CaptureRequest(CaptureMode Mode, CaptureSelectionMode SelectionMode);
