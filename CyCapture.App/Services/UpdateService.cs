using System.Buffers;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CyCapture.Services;

internal static class AppBuildInfo
{
    internal static Version Version { get; } = Normalize(typeof(AppBuildInfo).Assembly.GetName().Version);
    internal static string DisplayVersion => Version.ToString(3);
    internal static bool IncludesCyAnnota
    {
        get
        {
#if CYCAPTURE_CYANNOTA_PLUGIN
            return true;
#else
            return false;
#endif
        }
    }
    internal static bool IsInstalled =>
        File.Exists(Path.Combine(AppContext.BaseDirectory, "unins000.exe"));
    internal static string EditionLabel => IsInstalled
        ? "INSTALLÉE AVEC CYANNOTA"
        : IncludesCyAnnota ? "PORTABLE AVEC CYANNOTA" : "PORTABLE SANS CYANNOTA";

    private static Version Normalize(Version? version) => new(
        Math.Max(0, version?.Major ?? 0),
        Math.Max(0, version?.Minor ?? 0),
        Math.Max(0, version?.Build ?? 0));
}

internal sealed record UpdateCheckResult(
    Version LatestVersion,
    string Tag,
    string ReleasePageUrl,
    bool IsUpdateAvailable,
    string? AssetName,
    string? DownloadUrl,
    long AssetSize,
    string? Sha256);

internal sealed class UpdateService
{
    private const string LatestReleaseApi = "https://api.github.com/repos/MrMybal/CyCapture/releases/latest";
    private const long MaximumDownloadSize = 500L * 1024 * 1024;
    private static readonly HttpClient Client = CreateClient();

    internal async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        using var response = await Client.GetAsync(LatestReleaseApi, timeout.Token);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(timeout.Token);
        return ParseLatestRelease(json, AppBuildInfo.Version, AppBuildInfo.IncludesCyAnnota, AppBuildInfo.IsInstalled);
    }

    internal async Task DownloadAsync(
        UpdateCheckResult update,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        if (!update.IsUpdateAvailable || string.IsNullOrWhiteSpace(update.DownloadUrl)
            || string.IsNullOrWhiteSpace(update.Sha256) || string.IsNullOrWhiteSpace(update.AssetName))
            throw new InvalidOperationException("Cette release ne contient pas de téléchargement vérifiable pour cette édition.");
        if (update.AssetSize <= 0 || update.AssetSize > MaximumDownloadSize)
            throw new InvalidDataException("La taille annoncée du fichier de mise à jour est invalide.");

        var destination = Path.GetFullPath(destinationPath);
        if (string.Equals(destination, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Choisissez un autre nom : Windows ne peut pas remplacer CyCapture pendant son exécution.");

        var temporaryDirectory = Path.Combine(Path.GetTempPath(), "CyCapture", "Updates");
        Directory.CreateDirectory(temporaryDirectory);
        var temporaryPath = Path.Combine(temporaryDirectory, $"{Guid.NewGuid():N}.download");
        try
        {
            using var response = await Client.GetAsync(
                update.DownloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is > MaximumDownloadSize)
                throw new InvalidDataException("Le fichier de mise à jour dépasse la taille autorisée.");

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var destinationStream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                128 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = ArrayPool<byte>.Shared.Rent(128 * 1024);
            long total = 0;
            try
            {
                while (true)
                {
                    var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                    if (read == 0) break;
                    total += read;
                    if (total > MaximumDownloadSize)
                        throw new InvalidDataException("Le fichier de mise à jour dépasse la taille autorisée.");
                    hash.AppendData(buffer, 0, read);
                    await destinationStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            await destinationStream.FlushAsync(cancellationToken);

            if (total != update.AssetSize)
                throw new InvalidDataException("La taille du fichier téléchargé ne correspond pas à la release GitHub.");
            var actualHash = Convert.ToHexString(hash.GetHashAndReset());
            if (!actualHash.Equals(update.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("La vérification SHA-256 de la mise à jour a échoué.");

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Move(temporaryPath, destination, true);
        }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            catch { }
        }
    }

    internal static UpdateCheckResult ParseLatestRelease(
        string json,
        Version currentVersion,
        bool includesCyAnnota,
        bool installed = false)
    {
        var release = JsonSerializer.Deserialize<GitHubRelease>(json)
            ?? throw new InvalidDataException("La réponse GitHub est vide.");
        var tag = release.TagName?.Trim() ?? string.Empty;
        var versionText = tag.TrimStart('v', 'V');
        var suffix = versionText.IndexOfAny(['-', '+']);
        if (suffix >= 0) versionText = versionText[..suffix];
        if (!Version.TryParse(versionText, out var parsedVersion))
            throw new InvalidDataException("La version publiée par GitHub est invalide.");
        var latestVersion = new Version(
            Math.Max(0, parsedVersion.Major),
            Math.Max(0, parsedVersion.Minor),
            Math.Max(0, parsedVersion.Build));
        var releaseUrl = ValidatedGitHubUrl(release.HtmlUrl, $"/MrMybal/CyCapture/releases/tag/{tag}")
            ?? "https://github.com/MrMybal/CyCapture/releases/latest";
        var available = latestVersion > currentVersion;
        var expectedName = installed
            ? $"CyCapture-{latestVersion:3}-windows-x64-installer.exe"
            : $"CyCapture-{latestVersion:3}-windows-x64-portable"
              + (includesCyAnnota ? string.Empty : "-without-CyAnnota") + ".exe";
        var asset = release.Assets?.FirstOrDefault(item =>
            string.Equals(item.Name, expectedName, StringComparison.OrdinalIgnoreCase));
        var downloadUrl = asset is null
            ? null
            : ValidatedGitHubUrl(asset.DownloadUrl, $"/MrMybal/CyCapture/releases/download/{tag}/{expectedName}");
        var sha256 = ParseSha256(asset?.Digest);
        if (downloadUrl is null || sha256 is null || asset?.Size is not > 0)
        {
            asset = null;
            downloadUrl = null;
            sha256 = null;
        }

        return new UpdateCheckResult(
            latestVersion,
            tag,
            releaseUrl,
            available,
            asset?.Name,
            downloadUrl,
            asset?.Size ?? 0,
            sha256);
    }

    private static string? ValidatedGitHubUrl(string? value, string expectedPath)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return null;
        return uri.Scheme == Uri.UriSchemeHttps
               && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
               && uri.AbsolutePath.Equals(expectedPath, StringComparison.OrdinalIgnoreCase)
            ? uri.AbsoluteUri
            : null;
    }

    private static string? ParseSha256(string? digest)
    {
        const string prefix = "sha256:";
        if (digest is null || !digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
        var value = digest[prefix.Length..];
        return value.Length == 64 && value.All(Uri.IsHexDigit) ? value.ToUpperInvariant() : null;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CyCapture", AppBuildInfo.DisplayVersion));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; init; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; init; }

        [JsonPropertyName("assets")]
        public IReadOnlyList<GitHubAsset>? Assets { get; init; }
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("browser_download_url")]
        public string? DownloadUrl { get; init; }

        [JsonPropertyName("size")]
        public long Size { get; init; }

        [JsonPropertyName("digest")]
        public string? Digest { get; init; }
    }
}
