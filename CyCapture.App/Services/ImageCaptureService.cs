using System.Drawing.Imaging;
using CyCapture.Models;
using CyCapture.Platform.Windows;

namespace CyCapture.Services;

internal sealed class ImageCaptureService
{
    internal async Task<CaptureArtifact> CaptureAsync(CaptureSelection selection, Preferences preferences)
    {
        var startedAt = DateTimeOffset.Now;
        var outputDirectory = CaptureStorage.GetDirectory(preferences, CaptureMode.Image, startedAt);
        Directory.CreateDirectory(outputDirectory);
        var extension = preferences.ImageFormat == "jpeg" ? "jpg" : "png";
        var path = Path.Combine(outputDirectory, FileNames.Create("capture", extension));

        using var bitmap = await Task.Run(() => ScreenCapture.Capture(selection.Bounds));
        if (preferences.ImageFormat == "jpeg")
        {
            var encoder = ImageCodecInfo.GetImageEncoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
            using var parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)preferences.JpegQuality);
            bitmap.Save(path, encoder, parameters);
        }
        else
            bitmap.Save(path, ImageFormat.Png);

        if (preferences.CopyScreenshotsToClipboard)
        {
            try { ScreenCapture.CopyToClipboard(bitmap); }
            catch { /* L’image reste enregistrée même si une application verrouille le presse-papiers. */ }
        }

        return new CaptureArtifact(path, CaptureMode.Image, selection, startedAt, DateTimeOffset.Now);
    }
}

internal static class FileNames
{
    internal static string Create(string prefix, string extension) =>
        $"{prefix}-{DateTimeOffset.Now:yyyy-MM-ddTHH-mm-ss-fff}.{extension}";
}
