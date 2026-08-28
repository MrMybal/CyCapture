using System.Globalization;
using CyCapture.Models;

namespace CyCapture.Services;

internal static class CaptureStorage
{
    internal static string GetDirectory(
        Preferences preferences,
        CaptureMode mode,
        DateTimeOffset capturedAt)
    {
        var directory = preferences.EffectiveOutputDirectory;
        if (preferences.CreateDailyCaptureFolders)
            directory = Path.Combine(directory, capturedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        if (preferences.SeparateCaptureTypes)
            directory = Path.Combine(directory, GetModeDirectory(mode));
        return directory;
    }

    private static string GetModeDirectory(CaptureMode mode) => mode switch
    {
        CaptureMode.Image => "Images",
        CaptureMode.Video => "Videos",
        CaptureMode.Gif => "GIFs",
        CaptureMode.Audio => "Audio",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };
}
