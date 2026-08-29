using System.Drawing;
using System.Drawing.Drawing2D;
using CyCapture.Models;

namespace CyCapture.Services;

internal static class QuickAnnotationRenderer
{
    internal static void Render(Bitmap bitmap, CaptureSelection selection)
    {
        if (selection.QuickAnnotations is not { Elements.Count: > 0 } annotations) return;

        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        foreach (var element in annotations.Elements)
            DrawElement(graphics, element, selection);
    }

    private static void DrawElement(Graphics graphics, QuickAnnotationElement element, CaptureSelection selection)
    {
        if (element.Points.Count == 0) return;
        var scale = Math.Max(1f, (float)selection.Monitor.Scale);
        var color = ParseColor(element.Color);
        using var pen = new Pen(color, 3.2f * scale)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        var points = element.Points.Select(point => new PointF(
            point.X - selection.Bounds.X,
            point.Y - selection.Bounds.Y)).ToArray();

        switch (element.Kind)
        {
            case QuickAnnotationKind.Freehand when points.Length >= 2:
                graphics.DrawLines(pen, points);
                break;
            case QuickAnnotationKind.Rectangle when points.Length >= 2:
                var rectangle = Normalize(points[0], points[^1]);
                graphics.DrawRectangle(pen, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
                break;
            case QuickAnnotationKind.Arrow when points.Length >= 2:
                using (var arrow = new AdjustableArrowCap(5.5f * scale, 7.5f * scale, true))
                {
                    pen.CustomEndCap = arrow;
                    graphics.DrawLine(pen, points[0], points[^1]);
                }
                break;
            case QuickAnnotationKind.Text:
                DrawText(graphics, element.Text, points[0], color, scale);
                break;
        }
    }

    private static void DrawText(Graphics graphics, string text, PointF point, Color color, float scale)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        using var font = new Font("Segoe UI", 21f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
        var size = graphics.MeasureString(text, font);
        var padding = 5f * scale;
        using var background = new SolidBrush(Color.FromArgb(185, 5, 8, 8));
        using var foreground = new SolidBrush(color);
        graphics.FillRectangle(background, point.X - padding, point.Y - padding, size.Width + padding * 2, size.Height + padding * 2);
        graphics.DrawString(text, font, foreground, point);
    }

    private static RectangleF Normalize(PointF first, PointF second) => new(
        Math.Min(first.X, second.X),
        Math.Min(first.Y, second.Y),
        Math.Abs(second.X - first.X),
        Math.Abs(second.Y - first.Y));

    private static Color ParseColor(string value)
    {
        try { return ColorTranslator.FromHtml(value); }
        catch { return Color.FromArgb(255, 255, 70, 87); }
    }
}
