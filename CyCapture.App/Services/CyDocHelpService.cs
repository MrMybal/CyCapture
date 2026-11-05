using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace CyCapture.Services;

internal sealed record CyDocRoute(
    string PageId,
    string? Anchor = null,
    IReadOnlyDictionary<string, string>? State = null)
{
    internal static CyDocRoute Home { get; } = new("getting-started");
}

internal static class CyDocHelpService
{
    internal const string ResourceName = "CyCapture.Bundled.CyDocHelp.zip";
    private static readonly SemaphoreSlim ExtractionLock = new(1, 1);

    internal static string NormalizeLocale(string? locale) =>
        string.Equals(locale, "fr", StringComparison.OrdinalIgnoreCase) ? "fr" : "en";

    internal static Uri CreateUri(string indexPath, string? locale, CyDocRoute route)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(route.PageId);
        var builder = new StringBuilder(new Uri(Path.GetFullPath(indexPath)).AbsoluteUri)
            .Append("#/")
            .Append(Uri.EscapeDataString(NormalizeLocale(locale)))
            .Append('/')
            .Append(Uri.EscapeDataString(route.PageId));
        if (!string.IsNullOrWhiteSpace(route.Anchor))
            builder.Append('/').Append(Uri.EscapeDataString(route.Anchor));
        if (route.State is { Count: > 0 })
        {
            var first = true;
            foreach (var entry in route.State.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                builder.Append(first ? '?' : '&');
                first = false;
                builder.Append(Uri.EscapeDataString(entry.Key))
                    .Append('=')
                    .Append(Uri.EscapeDataString(entry.Value));
            }
        }
        return new Uri(builder.ToString());
    }

    internal static async Task<Uri> PrepareUriAsync(
        string? locale,
        CyDocRoute route,
        CancellationToken cancellationToken = default)
    {
        var indexPath = await EnsureExtractedAsync(cancellationToken);
        return CreateUri(indexPath, locale, route);
    }

    internal static async Task OpenAsync(
        string? locale,
        CyDocRoute route,
        CancellationToken cancellationToken = default)
    {
        var uri = await PrepareUriAsync(locale, route, cancellationToken);
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }

    private static async Task<string> EnsureExtractedAsync(CancellationToken cancellationToken)
    {
        await ExtractionLock.WaitAsync(cancellationToken);
        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "current";
            var helpRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CyCapture",
                "Help");
            var destination = Path.Combine(helpRoot, version);
            var indexPath = Path.Combine(destination, "index.html");
            var markerPath = Path.Combine(destination, ".complete");
            var fingerprint = Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId.ToString("N");
            if (File.Exists(indexPath) && File.Exists(markerPath)
                && string.Equals(await File.ReadAllTextAsync(markerPath, cancellationToken), fingerprint, StringComparison.Ordinal))
                return indexPath;

            Directory.CreateDirectory(helpRoot);
            var temporary = Path.Combine(helpRoot, $".{version}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(temporary);
            try
            {
                await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
                    ?? throw new InvalidOperationException("The bundled CyDoc help archive is missing.");
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var target = SafeEntryPath(temporary, entry.FullName);
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(target);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    await using var input = entry.Open();
                    await using var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
                    await input.CopyToAsync(output, cancellationToken);
                }

                foreach (var required in new[] { "index.html", "viewer.js", "viewer.css", "data.js", "manifest.json" })
                    if (!File.Exists(Path.Combine(temporary, required)))
                        throw new InvalidDataException($"The bundled CyDoc help is incomplete: {required} is missing.");

                await File.WriteAllTextAsync(Path.Combine(temporary, ".complete"), fingerprint, cancellationToken);
                if (Directory.Exists(destination)) Directory.Delete(destination, recursive: true);
                Directory.Move(temporary, destination);
            }
            finally
            {
                if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true);
            }
            return indexPath;
        }
        finally
        {
            ExtractionLock.Release();
        }
    }

    private static string SafeEntryPath(string root, string entryName)
    {
        var rootPrefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var target = Path.GetFullPath(Path.Combine(root, entryName.Replace('/', Path.DirectorySeparatorChar)));
        if (!target.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Unsafe entry in bundled CyDoc help: {entryName}");
        return target;
    }
}
