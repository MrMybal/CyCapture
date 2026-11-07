using BergamotTranslatorSharp;
using CyCapture.Models;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;
using Tesseract;

namespace CyCapture.Services;

internal sealed class OcrTranslationService
{
    private readonly TranslationModelService _models;

    internal OcrTranslationService(TranslationModelService models)
    {
        _models = models;
    }

    internal async Task<ScreenTranslationResult> TranslateAsync(
        CaptureSelection selection,
        Preferences preferences,
        IProgress<TranslationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var lines = await RecognizeAsync(selection, preferences, progress, cancellationToken);
        var requestedSource = TranslationLanguageCatalog.NormalizeSource(preferences.TranslationSourceLanguage);
        var target = TranslationLanguageCatalog.NormalizeTarget(preferences.TranslationTargetLanguage);
        var source = requestedSource == TranslationLanguageCatalog.AutomaticCode
            ? TranslationLanguageCatalog.DetectFromText(string.Join(' ', lines.Select(line => line.Text)), target == "en" ? "fr" : "en")
            : requestedSource;
        if (source == target)
        {
            return new ScreenTranslationResult(
                selection,
                selection.FrozenFramePng!,
                source,
                target,
                lines.Select(line => new TranslatedTextLine(line.Bounds, line.Text, line.Text, line.Confidence)).ToArray());
        }

        var configs = await _models.EnsureTranslationAsync(source, target, progress, cancellationToken);
        progress?.Report(new TranslationProgress(UiLocalization.Text(
            "Traduction locale du texte…",
            "Translating text locally…")));
        var translated = await Task.Run(() => TranslateLines(configs, lines), cancellationToken);
        var resultLines = lines.Zip(translated, (line, text) => new TranslatedTextLine(
            line.Bounds,
            line.Text,
            string.IsNullOrWhiteSpace(text) ? line.Text : text.Trim(),
            line.Confidence)).ToArray();
        return new ScreenTranslationResult(selection, selection.FrozenFramePng!, source, target, resultLines);
    }

    internal async Task<IReadOnlyList<OcrTextLine>> RecognizeAsync(
        CaptureSelection selection,
        Preferences preferences,
        IProgress<TranslationProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (selection.FrozenFramePng is not { Length: > 0 } frozenFrame)
            throw new InvalidOperationException(UiLocalization.Text(
                "L’image figée de la sélection n’est plus disponible.",
                "The frozen selection image is no longer available."));

        var requestedSource = TranslationLanguageCatalog.NormalizeSource(preferences.TranslationSourceLanguage);
        var engineLanguages = await _models.EnsureOcrAsync(requestedSource, progress, cancellationToken);
        progress?.Report(new TranslationProgress(UiLocalization.Text(
            "Reconnaissance du texte dans la zone…",
            "Recognizing text in the selected area…")));
        var lines = await Task.Run(
            () => Recognize(frozenFrame, selection.LocalBounds, engineLanguages, cancellationToken),
            cancellationToken);
        if (lines.Count == 0)
            throw new InvalidOperationException(UiLocalization.Text(
                "Aucun texte lisible n’a été trouvé dans cette zone.",
                "No readable text was found in this area."));
        return lines;
    }

    private static IReadOnlyList<OcrTextLine> Recognize(
        byte[] frozenFrame,
        PixelBounds localBounds,
        string engineLanguages,
        CancellationToken cancellationToken)
    {
        using var sourceStream = new MemoryStream(frozenFrame, writable: false);
        using var desktop = new DrawingBitmap(sourceStream);
        var safe = localBounds.Intersect(new PixelBounds(0, 0, desktop.Width, desktop.Height));
        if (safe.Width <= 0 || safe.Height <= 0)
            throw new InvalidOperationException("La zone sélectionnée est en dehors de l’image figée.");
        using var crop = desktop.Clone(
            new System.Drawing.Rectangle(safe.X, safe.Y, safe.Width, safe.Height),
            DrawingPixelFormat.Format32bppArgb);
        using var encoded = new MemoryStream();
        crop.Save(encoded, System.Drawing.Imaging.ImageFormat.Png);
        using var pix = Pix.LoadFromMemory(encoded.ToArray());
        using var engine = new TesseractEngine(TranslationModelService.OcrDirectory, engineLanguages, EngineMode.LstmOnly);
        engine.DefaultPageSegMode = PageSegMode.Auto;
        using var page = engine.Process(pix);
        using var iterator = page.GetIterator();
        iterator.Begin();
        var lines = new List<OcrTextLine>();
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = iterator.GetText(PageIteratorLevel.TextLine)?.Trim();
            if (string.IsNullOrWhiteSpace(text)
                || !iterator.TryGetBoundingBox(PageIteratorLevel.TextLine, out var bounds)) continue;
            var confidence = iterator.GetConfidence(PageIteratorLevel.TextLine);
            if (confidence < 8) continue;
            lines.Add(new OcrTextLine(
                new PixelBounds(bounds.X1, bounds.Y1, bounds.Width, bounds.Height),
                text,
                confidence));
        } while (iterator.Next(PageIteratorLevel.TextLine));

        if (lines.Count == 0)
        {
            var text = page.GetText()?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                lines.Add(new OcrTextLine(new PixelBounds(0, 0, safe.Width, safe.Height), text, page.GetMeanConfidence()));
        }
        return lines;
    }

    private static IReadOnlyList<string> TranslateLines(IReadOnlyList<string> configs, IReadOnlyList<OcrTextLine> lines)
    {
        if (configs.Count == 0) return lines.Select(line => line.Text).ToArray();
        using var service = configs.Count == 1
            ? new BlockingService(configs[0])
            : new BlockingService(configs[0], configs[1]);
        return service.Translate(lines.Select(line => line.Text)).ToArray();
    }
}
