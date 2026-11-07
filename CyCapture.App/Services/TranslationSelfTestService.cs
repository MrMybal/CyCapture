using System.Text.Json;
using CyCapture.Models;

namespace CyCapture.Services;

internal sealed class TranslationSelfTestService
{
    internal async Task RunAsync(string reportPath, CancellationToken cancellationToken = default)
    {
        var models = new TranslationModelService();
        var translation = new OcrTranslationService(models);
        byte[] png;
        using (var bitmap = new System.Drawing.Bitmap(720, 180))
        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
        using (var font = new System.Drawing.Font("Microsoft YaHei", 52, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel))
        using (var stream = new MemoryStream())
        {
            graphics.Clear(System.Drawing.Color.White);
            graphics.DrawString("你好，世界！", font, System.Drawing.Brushes.Black, new System.Drawing.PointF(24, 45));
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            png = stream.ToArray();
        }

        var bounds = new PixelBounds(0, 0, 720, 180);
        var monitor = new MonitorDescriptor(0, "SELFTEST", "Synthetic", bounds, bounds, 1, true);
        var selection = new CaptureSelection(
            bounds,
            monitor,
            SelectionKind.Region,
            "Synthetic Chinese",
            FrozenFramePng: png);
        var result = await translation.TranslateAsync(
            selection,
            new Preferences { TranslationSourceLanguage = "zh", TranslationTargetLanguage = "fr" },
            null,
            cancellationToken);
        if (result.Lines.Count == 0 || string.IsNullOrWhiteSpace(result.PlainText))
            throw new InvalidOperationException("Le test OCR/traduction n’a produit aucun texte.");

        var fullPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(
            fullPath,
            JsonSerializer.Serialize(new
            {
                passed = true,
                source = result.SourceLanguage,
                target = result.TargetLanguage,
                lineCount = result.Lines.Count,
                translatedText = result.PlainText
            }, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken);
    }
}
