namespace CyCapture.Models;

public sealed record TranslationLanguage(
    string Code,
    string EnglishName,
    string FrenchName,
    string TesseractCode)
{
    public string DisplayName => Services.UiLocalization.IsFrench ? FrenchName : EnglishName;
    public override string ToString() => DisplayName;
}

public sealed record TranslationProgress(string Message, double? Fraction = null);

public sealed record OcrTextLine(
    PixelBounds Bounds,
    string Text,
    float Confidence);

public sealed record TranslatedTextLine(
    PixelBounds Bounds,
    string OriginalText,
    string TranslatedText,
    float Confidence);

public sealed record ScreenTranslationResult(
    CaptureSelection Selection,
    byte[] FrozenDesktopPng,
    string SourceLanguage,
    string TargetLanguage,
    IReadOnlyList<TranslatedTextLine> Lines)
{
    public string PlainText => string.Join(Environment.NewLine, Lines.Select(line => line.TranslatedText));
}
