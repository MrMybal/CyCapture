using System.Text.Json;
using CyCapture.Models;

namespace CyCapture.Services;

internal sealed class HistoryService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".webp"
    };
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".mkv", ".avi", ".webm"
    };
    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".m4a", ".aac", ".flac", ".wma", ".ogg"
    };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string PathName => Path.Combine(Preferences.DataDirectory, "history.json");

    internal async Task AddAsync(CaptureArtifact artifact)
    {
        await _gate.WaitAsync();
        try
        {
            Directory.CreateDirectory(Preferences.DataDirectory);
            var entries = await ReadAsync();
            entries.Insert(0, new HistoryEntry(
                artifact.Path,
                artifact.Mode,
                artifact.FinishedAt,
                artifact.Selection.Bounds.Width,
                artifact.Selection.Bounds.Height));
            if (entries.Count > 100) entries.RemoveRange(100, entries.Count - 100);
            await using var stream = File.Create(PathName);
            await JsonSerializer.SerializeAsync(stream, entries, new JsonSerializerOptions { WriteIndented = true });
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task<List<HistoryEntry>> ReadAsync()
    {
        try
        {
            await using var stream = File.OpenRead(PathName);
            return await JsonSerializer.DeserializeAsync<List<HistoryEntry>>(stream) ?? [];
        }
        catch
        {
            return [];
        }
    }

    internal async Task<List<HistoryEntry>> ReadAvailableAsync(string outputDirectory, bool includeHistory = true)
    {
        var entries = includeHistory ? await ReadAsync() : [];
        var available = new Dictionary<string, HistoryEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries.Where(entry => File.Exists(entry.Path)))
            available[Path.GetFullPath(entry.Path)] = entry;

        try
        {
            Directory.CreateDirectory(outputDirectory);
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            };
            foreach (var file in Directory.EnumerateFiles(outputDirectory, "*", options))
            {
                var mode = GetMode(file);
                if (mode is null) continue;
                var fullPath = Path.GetFullPath(file);
                if (available.ContainsKey(fullPath)) continue;
                var info = new FileInfo(fullPath);
                available[fullPath] = new HistoryEntry(
                    fullPath,
                    mode.Value,
                    info.LastWriteTimeUtc,
                    0,
                    0);
            }
        }
        catch
        {
            // The existing history remains usable if a removable or protected folder is unavailable.
        }

        return available.Values
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(200)
            .ToList();
    }

    private static CaptureMode? GetMode(string path)
    {
        var extension = Path.GetExtension(path);
        if (extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)) return CaptureMode.Gif;
        if (AudioExtensions.Contains(extension)) return CaptureMode.Audio;
        if (VideoExtensions.Contains(extension)) return CaptureMode.Video;
        return ImageExtensions.Contains(extension) ? CaptureMode.Image : null;
    }
}

internal sealed record HistoryEntry(string Path, CaptureMode Mode, DateTimeOffset CreatedAt, int Width, int Height);
