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
        var documentationRoot = Path.Combine(AppContext.BaseDirectory, "Documentation");
        var documentationPath = Path.Combine(documentationRoot, "Documentation.html");
        foreach (var required in new[]
                 {
                     documentationPath,
                     Path.Combine(documentationRoot, "DocAI.md"),
                     Path.Combine(documentationRoot, "DocData", "data.js"),
                     Path.Combine(documentationRoot, "DocData", "viewer.js"),
                     Path.Combine(documentationRoot, "DocData", "viewer.css"),
                     Path.Combine(documentationRoot, "DocData", "manifest.json")
                 })
        {
            if (!File.Exists(required))
                throw new FileNotFoundException("The local CyDoc documentation distribution is incomplete.", required);
        }
        return documentationPath;
    }
}
