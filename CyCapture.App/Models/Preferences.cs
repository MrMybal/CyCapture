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
    ShowModePicker,
    CaptureImage,
    CaptureVideo,
    CaptureGif
}

public sealed class Preferences
{
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
    public PrintScreenBehavior PrintScreenBehavior { get; set; } = PrintScreenBehavior.ShowModePicker;
    public bool IncludeSystemAudio { get; set; } = true;
    public bool IncludeMicrophone { get; set; }
    public bool CopyScreenshotsToClipboard { get; set; } = true;
    public bool CopyVideosToClipboard { get; set; }
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
        value.ApplyEncodingProfiles();
        if (!Enum.IsDefined(value.GifQuality)) value.GifQuality = GifQuality.Balanced;
        if (!Enum.IsDefined(value.SelectionMode)) value.SelectionMode = CaptureSelectionMode.Smart;
        if (!Enum.IsDefined(value.PrintScreenBehavior))
            value.PrintScreenBehavior = PrintScreenBehavior.ShowModePicker;
        if (!string.IsNullOrWhiteSpace(value.OutputDirectory) && !Path.IsPathFullyQualified(value.OutputDirectory))
            value.OutputDirectory = null;
        return value;
    }
}
