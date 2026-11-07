using System.Text.Json;

namespace CyCapture.Models;

public enum GifQuality
{
    Compact,
    Balanced,
    High
}

public enum VideoQuality
{
    Compact,
    Balanced,
    High
}

public enum ImageEncodingQuality
{
    Compact,
    Balanced,
    High
}

public enum VideoEncodingQuality
{
    VeryLow,
    Compact,
    Balanced,
    High
}

public enum AudioEncodingQuality
{
    Compact,
    Balanced,
    High
}

public enum PrintScreenBehavior
{
    ShowModePicker = 0,
    CaptureImage = 1,
    CaptureVideo = 2,
    CaptureGif = 3,
    CaptureAudio = 4,
    CaptureTranslation = 5,
    QuickImageHoldSelection = 6,
    CaptureText = 7
}

public sealed class Preferences
{
    private const int CurrentPrintScreenBehaviorSchemaVersion = 1;

    public string? OutputDirectory { get; set; }
    public string ImageFormat { get; set; } = "png";
    public int JpegQuality { get; set; } = 70;
    public ImageEncodingQuality ImageEncodingQuality { get; set; } = ImageEncodingQuality.Balanced;
    public int FramesPerSecond { get; set; } = 30;
    public int VideoBitrate { get; set; } = 6_000_000;
    public VideoQuality VideoQualityLevel { get; set; } = VideoQuality.Balanced;
    public VideoEncodingQuality VideoEncodingQuality { get; set; } = VideoEncodingQuality.Balanced;
    public AudioEncodingQuality AudioEncodingQuality { get; set; } = AudioEncodingQuality.Balanced;
    public GifQuality GifQuality { get; set; } = GifQuality.Balanced;
    public CaptureSelectionMode SelectionMode { get; set; } = CaptureSelectionMode.Smart;
    public PrintScreenBehavior PrintScreenBehavior { get; set; } = PrintScreenBehavior.QuickImageHoldSelection;
    public int? PrintScreenBehaviorSchemaVersion { get; set; }
    public int PrintScreenHoldDelayMilliseconds { get; set; } = 300;
    public bool IncludeSystemAudio { get; set; } = true;
    public bool IncludeMicrophone { get; set; }
    // Null means this preference predates per-mode settings; inherit the old value once on load.
    public bool? AudioOnlyIncludeSystemAudio { get; set; }
    public bool? AudioOnlyIncludeMicrophone { get; set; }
    public AudioEncodingQuality? AudioOnlyEncodingQuality { get; set; }
    public bool AudioOnlyIgnoreSystemVolume { get; set; } = true;
    public bool CopyScreenshotsToClipboard { get; set; } = true;
    public bool CopyVideosToClipboard { get; set; } = true;
    public bool CopyGifsToClipboard { get; set; } = true;
    public bool CopyAudioToClipboard { get; set; } = true;
    public bool CopyTranslationsToClipboard { get; set; } = true;
    // Null identifies preferences saved before each capture mode had its own clipboard option.
    public bool? ClipboardOptionsInitialized { get; set; }
    public bool EnableQuickAnnotations { get; set; } = true;
    public bool ShowRecordingFrame { get; set; } = true;
    public bool? ShowGifRecordingFrame { get; set; }
    public bool SeparateCaptureTypes { get; set; }
    public bool CreateDailyCaptureFolders { get; set; }
    public bool StartWithWindows { get; set; }
    public string ApplicationLanguage { get; set; } = "fr";
    public string DocumentationLanguage { get; set; } = "en";
    public string TranslationSourceLanguage { get; set; } = "auto";
    public string TranslationTargetLanguage { get; set; } = "fr";

    public static string DataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CyCapture");

    public string EffectiveOutputDirectory => string.IsNullOrWhiteSpace(OutputDirectory)
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "CyCapture")
        : OutputDirectory;

    public static async Task<Preferences> LoadAsync()
    {
        var path = Path.Combine(DataDirectory, "preferences.json");
        try
        {
            await using var stream = File.OpenRead(path);
            return Sanitize(await JsonSerializer.DeserializeAsync<Preferences>(stream) ?? new Preferences());
        }
        catch
        {
            return Sanitize(new Preferences());
        }
    }

    public async Task SaveAsync()
    {
        Directory.CreateDirectory(DataDirectory);
        await using var stream = File.Create(Path.Combine(DataDirectory, "preferences.json"));
        await JsonSerializer.SerializeAsync(stream, Sanitize(this), new JsonSerializerOptions { WriteIndented = true });
    }

    internal (bool SystemAudio, bool Microphone, AudioEncodingQuality Quality) GetAudioSettings(CaptureMode mode) => mode switch
    {
        CaptureMode.Video => (IncludeSystemAudio, IncludeMicrophone, AudioEncodingQuality),
        CaptureMode.Audio => (
            AudioOnlyIncludeSystemAudio ?? IncludeSystemAudio,
            AudioOnlyIncludeMicrophone ?? IncludeMicrophone,
            AudioOnlyEncodingQuality ?? AudioEncodingQuality),
        _ => (false, false, AudioEncodingQuality.Balanced)
    };

    internal bool ShouldShowRecordingFrame(CaptureMode mode) => mode switch
    {
        CaptureMode.Video => ShowRecordingFrame,
        CaptureMode.Gif => ShowGifRecordingFrame ?? ShowRecordingFrame,
        _ => false
    };

    internal void ApplyEncodingProfiles()
    {
        FramesPerSecond = VideoQualityLevel switch
        {
            VideoQuality.Compact => 24,
            VideoQuality.High => 60,
            _ => 30
        };
        VideoBitrate = VideoEncodingQuality switch
        {
            VideoEncodingQuality.VeryLow => 1_000_000,
            VideoEncodingQuality.Compact => 2_500_000,
            VideoEncodingQuality.High => 12_000_000,
            _ => 6_000_000
        };
        JpegQuality = ImageEncodingQuality switch
        {
            ImageEncodingQuality.Compact => 40,
            ImageEncodingQuality.High => 90,
            _ => 70
        };
    }

    private static Preferences Sanitize(Preferences value)
    {
        value.ImageFormat = value.ImageFormat.Equals("jpeg", StringComparison.OrdinalIgnoreCase) ? "jpeg" : "png";
        if (!Enum.IsDefined(value.ImageEncodingQuality)) value.ImageEncodingQuality = ImageEncodingQuality.Balanced;
        if (!Enum.IsDefined(value.VideoQualityLevel)) value.VideoQualityLevel = VideoQuality.Balanced;
        if (!Enum.IsDefined(value.VideoEncodingQuality)) value.VideoEncodingQuality = VideoEncodingQuality.Balanced;
        if (!Enum.IsDefined(value.AudioEncodingQuality)) value.AudioEncodingQuality = AudioEncodingQuality.Balanced;
        value.AudioOnlyIncludeSystemAudio ??= value.IncludeSystemAudio;
        value.AudioOnlyIncludeMicrophone ??= value.IncludeMicrophone;
        value.AudioOnlyEncodingQuality ??= value.AudioEncodingQuality;
        if (!Enum.IsDefined(value.AudioOnlyEncodingQuality.Value))
            value.AudioOnlyEncodingQuality = AudioEncodingQuality.Balanced;
        value.ShowGifRecordingFrame ??= value.ShowRecordingFrame;
        if (value.ClipboardOptionsInitialized != true)
        {
            value.CopyScreenshotsToClipboard = true;
            value.CopyVideosToClipboard = true;
            value.CopyGifsToClipboard = true;
            value.CopyAudioToClipboard = true;
            value.CopyTranslationsToClipboard = true;
            value.ClipboardOptionsInitialized = true;
        }
        value.ApplyEncodingProfiles();
        if (!Enum.IsDefined(value.GifQuality)) value.GifQuality = GifQuality.Balanced;
        if (!Enum.IsDefined(value.SelectionMode)) value.SelectionMode = CaptureSelectionMode.Smart;
        // Before screen translation was added, the numeric value 5 represented the hybrid
        // Print Screen workflow. Version 1.5 temporarily interpreted that saved value as
        // CaptureTranslation. Migrate unversioned preferences once, while preserving an
        // explicitly selected translation mode in files written with the new schema.
        if (value.PrintScreenBehaviorSchemaVersion.GetValueOrDefault() < CurrentPrintScreenBehaviorSchemaVersion)
        {
            if (value.PrintScreenBehavior == PrintScreenBehavior.CaptureTranslation)
                value.PrintScreenBehavior = PrintScreenBehavior.QuickImageHoldSelection;
            value.PrintScreenBehaviorSchemaVersion = CurrentPrintScreenBehaviorSchemaVersion;
        }
        if (!Enum.IsDefined(value.PrintScreenBehavior))
            value.PrintScreenBehavior = PrintScreenBehavior.QuickImageHoldSelection;
        var clampedHoldDelay = Math.Clamp(value.PrintScreenHoldDelayMilliseconds, 100, 1_000);
        value.PrintScreenHoldDelayMilliseconds = ((clampedHoldDelay + 50) / 100) * 100;
        if (!string.IsNullOrWhiteSpace(value.OutputDirectory) && !Path.IsPathFullyQualified(value.OutputDirectory))
            value.OutputDirectory = null;
        value.ApplicationLanguage = NormalizeLanguage(value.ApplicationLanguage, "fr");
        value.DocumentationLanguage = string.Equals(value.DocumentationLanguage, "fr", StringComparison.OrdinalIgnoreCase)
            ? "fr"
            : "en";
        value.TranslationSourceLanguage = Services.TranslationLanguageCatalog.NormalizeSource(value.TranslationSourceLanguage);
        value.TranslationTargetLanguage = Services.TranslationLanguageCatalog.NormalizeTarget(value.TranslationTargetLanguage);
        return value;
    }

    internal static string NormalizeLanguage(string? language, string fallback = "en") =>
        string.Equals(language, "fr", StringComparison.OrdinalIgnoreCase)
            ? "fr"
            : string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : fallback;
}
