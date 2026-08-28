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

public enum PrintScreenBehavior
{
    ShowModePicker,
    CaptureImage,
    CaptureVideo,
    CaptureGif
}

public sealed class Preferences
{
    public string? OutputDirectory { get; set; }
    public string ImageFormat { get; set; } = "png";
    public int FramesPerSecond { get; set; } = 30;
    public int VideoBitrate { get; set; } = 8_000_000;
    public VideoQuality VideoQualityLevel { get; set; } = VideoQuality.Balanced;
    public GifQuality GifQuality { get; set; } = GifQuality.Balanced;
    public CaptureSelectionMode SelectionMode { get; set; } = CaptureSelectionMode.Smart;
    public PrintScreenBehavior PrintScreenBehavior { get; set; } = PrintScreenBehavior.ShowModePicker;
    public bool IncludeSystemAudio { get; set; } = true;
    public bool IncludeMicrophone { get; set; }
    public bool CopyScreenshotsToClipboard { get; set; } = true;
    public bool ShowRecordingFrame { get; set; } = true;

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
            return new Preferences();
        }
    }

    public async Task SaveAsync()
    {
        Directory.CreateDirectory(DataDirectory);
        await using var stream = File.Create(Path.Combine(DataDirectory, "preferences.json"));
        await JsonSerializer.SerializeAsync(stream, Sanitize(this), new JsonSerializerOptions { WriteIndented = true });
    }

    internal void ApplyVideoQuality()
    {
        (FramesPerSecond, VideoBitrate) = VideoQualityLevel switch
        {
            VideoQuality.Compact => (24, 4_000_000),
            VideoQuality.High => (60, 16_000_000),
            _ => (30, 8_000_000)
        };
    }

    private static Preferences Sanitize(Preferences value)
    {
        value.ImageFormat = value.ImageFormat.Equals("jpeg", StringComparison.OrdinalIgnoreCase) ? "jpeg" : "png";
        if (!Enum.IsDefined(value.VideoQualityLevel)) value.VideoQualityLevel = VideoQuality.Balanced;
        value.ApplyVideoQuality();
        if (!Enum.IsDefined(value.GifQuality)) value.GifQuality = GifQuality.Balanced;
        if (!Enum.IsDefined(value.SelectionMode)) value.SelectionMode = CaptureSelectionMode.Smart;
        if (!Enum.IsDefined(value.PrintScreenBehavior))
            value.PrintScreenBehavior = PrintScreenBehavior.ShowModePicker;
        if (!string.IsNullOrWhiteSpace(value.OutputDirectory) && !Path.IsPathFullyQualified(value.OutputDirectory))
            value.OutputDirectory = null;
        return value;
    }
}
