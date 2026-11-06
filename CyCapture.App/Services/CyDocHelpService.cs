using System.Diagnostics;
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
    private const string BundledDocumentationPrefix = "CyCapture.Bundled.Documentation/";

    internal static string NormalizeLocale(string? locale) =>
        string.Equals(locale, "fr", StringComparison.OrdinalIgnoreCase) ? "fr" : "en";

    internal static Uri CreateUri(string documentationPath, string? locale, CyDocRoute route)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(route.PageId);
        var builder = new StringBuilder(new Uri(Path.GetFullPath(documentationPath)).AbsoluteUri)
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

    internal static Task<Uri> PrepareUriAsync(
        string? locale,
        CyDocRoute route,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateUri(ResolveDocumentationPath(), locale, route));
    }

    internal static async Task OpenAsync(
        string? locale,
        CyDocRoute route,
        CancellationToken cancellationToken = default)
    {
        var uri = await PrepareUriAsync(locale, route, cancellationToken);
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }

    private static string ResolveDocumentationPath()
    {
        var distributedRoot = Path.Combine(AppContext.BaseDirectory, "Documentation");
        if (IsComplete(distributedRoot))
            return Path.Combine(distributedRoot, "Documentation.html");

        var assembly = typeof(CyDocHelpService).Assembly;
        var resources = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(BundledDocumentationPrefix, StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (resources.Length == 0)
            throw new FileNotFoundException("The local CyDoc documentation distribution is incomplete.",
                Path.Combine(distributedRoot, "Documentation.html"));

        var version = assembly.GetName().Version?.ToString(3) ?? "current";
        var extractedRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CyCapture", "Bundled", "Documentation", version);
        ExtractBundledDocumentation(assembly, resources, extractedRoot);
        if (!IsComplete(extractedRoot))
            throw new FileNotFoundException("The bundled CyDoc documentation distribution is incomplete.",
                Path.Combine(extractedRoot, "Documentation.html"));
        return Path.Combine(extractedRoot, "Documentation.html");
    }

    private static bool IsComplete(string root)
    {
        foreach (var relativePath in new[]
                 {
                     "Documentation.html",
                     "DocAI.md",
                     Path.Combine("DocData", "data.js"),
                     Path.Combine("DocData", "viewer.js"),
                     Path.Combine("DocData", "viewer.css"),
                     Path.Combine("DocData", "manifest.json")
                 })
        {
            if (!File.Exists(Path.Combine(root, relativePath)))
                return false;
        }
        return true;
    }

    private static void ExtractBundledDocumentation(
        System.Reflection.Assembly assembly,
        IReadOnlyList<string> resources,
        string destinationRoot)
    {
        var normalizedRoot = Path.GetFullPath(destinationRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(normalizedRoot);
        foreach (var resourceName in resources)
        {
            var relativePath = resourceName[BundledDocumentationPrefix.Length..]
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            var targetPath = Path.GetFullPath(Path.Combine(normalizedRoot, relativePath));
            if (!targetPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("A bundled documentation resource has an invalid path.");

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            using var source = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidDataException($"Bundled documentation resource not found: {resourceName}");
            using var target = File.Create(targetPath);
            source.CopyTo(target);
        }
    }
}
