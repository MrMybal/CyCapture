using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CyCapture.Models;

namespace CyCapture.Services;

internal sealed class TranslationModelService
{
    private const string MozillaRegistryUrl =
        "https://storage.googleapis.com/moz-fx-translations-data--303e-prod-translations-data/db/models.json";
    private const string TesseractBaseUrl =
        "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/";
    private static readonly HttpClient Http = CreateHttpClient();
    private readonly SemaphoreSlim _registryGate = new(1, 1);
    private JsonDocument? _registry;

    internal static string ModelsDirectory => Path.Combine(Preferences.DataDirectory, "TranslationModels");
    internal static string OcrDirectory => Path.Combine(ModelsDirectory, "ocr");
    internal static string TranslationDirectory => Path.Combine(ModelsDirectory, "bergamot");

    internal bool IsOcrInstalled(string languageCode)
    {
        var language = TranslationLanguageCatalog.Find(languageCode);
        return language is not null && File.Exists(Path.Combine(OcrDirectory, language.TesseractCode + ".traineddata"));
    }

    internal bool IsDirectionInstalled(string sourceLanguage, string targetLanguage) =>
        IsConfigReady(ConfigPath(Direction(sourceLanguage, targetLanguage)));

    internal IReadOnlyList<string> InstalledOcrLanguages() => TranslationLanguageCatalog.Languages
        .Where(language => IsOcrInstalled(language.Code))
        .Select(language => language.Code)
        .ToArray();

    internal IReadOnlyList<string> InstalledDirections()
    {
        if (!Directory.Exists(TranslationDirectory)) return [];
        return Directory.EnumerateDirectories(TranslationDirectory)
            .Where(directory => IsConfigReady(Path.Combine(directory, "config.yml")))
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal async Task<string> EnsureOcrAsync(
        string sourceLanguage,
        IProgress<TranslationProgress>? progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(OcrDirectory);
        var requested = sourceLanguage.Equals(TranslationLanguageCatalog.AutomaticCode, StringComparison.OrdinalIgnoreCase)
            ? new[] { "en", "zh", "zh_hant", "ja", "ko" }
            : new[] { TranslationLanguageCatalog.NormalizeSource(sourceLanguage) };

        var models = new List<string>();
        foreach (var code in requested.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var language = TranslationLanguageCatalog.Find(code)
                           ?? throw new InvalidOperationException($"Langue OCR non prise en charge : {code}");
            var destination = Path.Combine(OcrDirectory, language.TesseractCode + ".traineddata");
            if (!File.Exists(destination))
            {
                progress?.Report(new TranslationProgress(UiLocalization.Text(
                    $"Téléchargement du modèle OCR · {language.FrenchName}",
                    $"Downloading OCR model · {language.EnglishName}")));
                await DownloadFileAsync(
                    new Uri(TesseractBaseUrl + Uri.EscapeDataString(language.TesseractCode) + ".traineddata"),
                    destination,
                    progress,
                    cancellationToken);
            }
            models.Add(language.TesseractCode);
        }
        return string.Join('+', models);
    }

    internal async Task<IReadOnlyList<string>> EnsureTranslationAsync(
        string sourceLanguage,
        string targetLanguage,
        IProgress<TranslationProgress>? progress,
        CancellationToken cancellationToken)
    {
        sourceLanguage = TranslationLanguageCatalog.NormalizeTarget(sourceLanguage);
        targetLanguage = TranslationLanguageCatalog.NormalizeTarget(targetLanguage);
        if (sourceLanguage == targetLanguage) return [];

        var directConfig = ConfigPath(Direction(sourceLanguage, targetLanguage));
        if (IsConfigReady(directConfig)) return [directConfig];
        if (sourceLanguage != "en" && targetLanguage != "en")
        {
            var sourceToEnglish = ConfigPath(Direction(sourceLanguage, "en"));
            var englishToTarget = ConfigPath(Direction("en", targetLanguage));
            if (IsConfigReady(sourceToEnglish) && IsConfigReady(englishToTarget))
                return [sourceToEnglish, englishToTarget];
        }

        var registry = await GetRegistryAsync(progress, cancellationToken);
        if (TryGetDirection(registry.RootElement, Direction(sourceLanguage, targetLanguage), out _))
            return [await EnsureDirectionAsync(sourceLanguage, targetLanguage, registry.RootElement, progress, cancellationToken)];

        if (sourceLanguage == "en" || targetLanguage == "en")
            throw UnsupportedPair(sourceLanguage, targetLanguage);

        if (!TryGetDirection(registry.RootElement, Direction(sourceLanguage, "en"), out _)
            || !TryGetDirection(registry.RootElement, Direction("en", targetLanguage), out _))
            throw UnsupportedPair(sourceLanguage, targetLanguage);

        return
        [
            await EnsureDirectionAsync(sourceLanguage, "en", registry.RootElement, progress, cancellationToken),
            await EnsureDirectionAsync("en", targetLanguage, registry.RootElement, progress, cancellationToken)
        ];
    }

    internal async Task DownloadLanguagePairAsync(
        string sourceLanguage,
        string targetLanguage,
        IProgress<TranslationProgress>? progress,
        CancellationToken cancellationToken)
    {
        await EnsureOcrAsync(sourceLanguage, progress, cancellationToken);
        await EnsureTranslationAsync(sourceLanguage, targetLanguage, progress, cancellationToken);
    }

    internal void RemoveOcrLanguage(string languageCode)
    {
        var language = TranslationLanguageCatalog.Find(languageCode);
        if (language is null) return;
        var path = Path.Combine(OcrDirectory, language.TesseractCode + ".traineddata");
        if (File.Exists(path)) File.Delete(path);
    }

    internal void RemoveDirection(string direction)
    {
        if (string.IsNullOrWhiteSpace(direction) || direction.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return;
        var root = Path.GetFullPath(TranslationDirectory) + Path.DirectorySeparatorChar;
        var target = Path.GetFullPath(Path.Combine(TranslationDirectory, direction));
        if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(target)) return;
        Directory.Delete(target, true);
    }

    private async Task<string> EnsureDirectionAsync(
        string sourceLanguage,
        string targetLanguage,
        JsonElement registry,
        IProgress<TranslationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var direction = Direction(sourceLanguage, targetLanguage);
        var configPath = ConfigPath(direction);
        if (IsConfigReady(configPath))
            return configPath;
        if (!TryGetDirection(registry, direction, out var candidates))
            throw UnsupportedPair(sourceLanguage, targetLanguage);

        var selected = SelectCandidate(candidates);
        var baseUrl = registry.GetProperty("baseUrl").GetString()
                      ?? throw new InvalidDataException("Le registre Mozilla ne contient pas de baseUrl.");
        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri)
            || !baseUri.Host.Equals("storage.googleapis.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Le registre Mozilla utilise une adresse de modèles non approuvée.");

        var directory = Path.Combine(TranslationDirectory, direction);
        Directory.CreateDirectory(directory);
        var files = selected.GetProperty("files");
        var downloaded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var propertyName in new[] { "model", "vocab", "srcVocab", "trgVocab", "lexicalShortlist" })
        {
            if (!files.TryGetProperty(propertyName, out var descriptor)
                || !descriptor.TryGetProperty("path", out var pathElement)
                || string.IsNullOrWhiteSpace(pathElement.GetString())) continue;
            var relativePath = pathElement.GetString()!;
            var fileName = Path.GetFileName(relativePath);
            var outputName = fileName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
                ? fileName[..^3]
                : fileName;
            var outputPath = Path.Combine(directory, outputName);
            if (!File.Exists(outputPath))
            {
                progress?.Report(new TranslationProgress(UiLocalization.Text(
                    $"Téléchargement traduction {TranslationLanguageCatalog.DisplayName(sourceLanguage)} → {TranslationLanguageCatalog.DisplayName(targetLanguage)} · {propertyName}",
                    $"Downloading translation {TranslationLanguageCatalog.DisplayName(sourceLanguage)} → {TranslationLanguageCatalog.DisplayName(targetLanguage)} · {propertyName}")));
                var uri = new Uri(baseUri, relativePath);
                if (!uri.Host.Equals(baseUri.Host, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Un fichier de modèle pointe vers un hôte non approuvé.");
                await DownloadModelFileAsync(uri, outputPath, fileName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase), progress, cancellationToken);
                if (descriptor.TryGetProperty("uncompressedHash", out var hashElement)
                    && !string.IsNullOrWhiteSpace(hashElement.GetString()))
                    VerifySha256(outputPath, hashElement.GetString()!);
            }
            downloaded[propertyName] = outputName;
        }

        if (!downloaded.TryGetValue("model", out var model)
            || !downloaded.TryGetValue("lexicalShortlist", out var shortlist))
            throw new InvalidDataException($"Le modèle {direction} est incomplet.");
        var vocabs = downloaded.TryGetValue("vocab", out var vocab)
            ? new[] { vocab, vocab }
            : new[]
            {
                downloaded.GetValueOrDefault("srcVocab") ?? throw new InvalidDataException("Vocabulaire source absent."),
                downloaded.GetValueOrDefault("trgVocab") ?? throw new InvalidDataException("Vocabulaire cible absent.")
            };
        var precision = model.Contains("alphas", StringComparison.OrdinalIgnoreCase)
            ? "int8shiftAlphaAll"
            : "int8shiftAll";
        var config = BuildConfig(model, vocabs, shortlist, precision);
        await File.WriteAllTextAsync(configPath + ".tmp", config, new UTF8Encoding(false), cancellationToken);
        File.Move(configPath + ".tmp", configPath, true);
        return configPath;
    }

    private async Task<JsonDocument> GetRegistryAsync(IProgress<TranslationProgress>? progress, CancellationToken cancellationToken)
    {
        if (_registry is not null) return _registry;
        await _registryGate.WaitAsync(cancellationToken);
        try
        {
            if (_registry is not null) return _registry;
            Directory.CreateDirectory(ModelsDirectory);
            var cachePath = Path.Combine(ModelsDirectory, "mozilla-models.json");
            if (File.Exists(cachePath))
            {
                _registry = JsonDocument.Parse(await File.ReadAllBytesAsync(cachePath, cancellationToken));
                return _registry;
            }
            try
            {
                progress?.Report(new TranslationProgress(UiLocalization.Text(
                    "Mise à jour du catalogue de langues…",
                    "Updating the language catalog…")));
                await DownloadFileAsync(new Uri(MozillaRegistryUrl), cachePath, progress, cancellationToken);
            }
            catch when (File.Exists(cachePath))
            {
                // The cached public model registry keeps installed translation available offline.
            }
            _registry = JsonDocument.Parse(await File.ReadAllBytesAsync(cachePath, cancellationToken));
            return _registry;
        }
        finally
        {
            _registryGate.Release();
        }
    }

    private static JsonElement SelectCandidate(JsonElement candidates)
    {
        JsonElement? fallback = null;
        foreach (var candidate in candidates.EnumerateArray())
        {
            fallback = candidate;
            if (candidate.TryGetProperty("releaseStatus", out var status)
                && status.ValueKind == JsonValueKind.String
                && status.GetString()?.Equals("Release", StringComparison.OrdinalIgnoreCase) == true)
                return candidate;
        }
        return fallback ?? throw new InvalidDataException("Aucun modèle n’est disponible pour cette direction.");
    }

    private static bool TryGetDirection(JsonElement registry, string direction, out JsonElement candidates)
    {
        candidates = default;
        return registry.TryGetProperty("models", out var models)
               && models.TryGetProperty(direction, out candidates)
               && candidates.ValueKind == JsonValueKind.Array
               && candidates.GetArrayLength() > 0;
    }

    private static async Task DownloadModelFileAsync(
        Uri uri,
        string outputPath,
        bool compressed,
        IProgress<TranslationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var temporary = outputPath + ".download";
        if (File.Exists(temporary)) File.Delete(temporary);
        using var response = await Http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var raw = compressed ? new GZipStream(input, CompressionMode.Decompress) : input;
        await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
        {
            await raw.CopyToAsync(output, cancellationToken);
            await output.FlushAsync(cancellationToken);
        }
        File.Move(temporary, outputPath, true);
    }

    private static async Task DownloadFileAsync(
        Uri uri,
        string outputPath,
        IProgress<TranslationProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (uri.Scheme != Uri.UriSchemeHttps) throw new InvalidDataException("Seuls les téléchargements HTTPS sont autorisés.");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var temporary = outputPath + ".download";
        if (File.Exists(temporary)) File.Delete(temporary);
        using var response = await Http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var length = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
        {
            var buffer = new byte[81920];
            long total = 0;
            while (true)
            {
                var read = await input.ReadAsync(buffer, cancellationToken);
                if (read == 0) break;
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                total += read;
                if (length > 0) progress?.Report(new TranslationProgress(string.Empty, (double)total / length.Value));
            }
            await output.FlushAsync(cancellationToken);
        }
        File.Move(temporary, outputPath, true);
    }

    private static string BuildConfig(string model, IReadOnlyList<string> vocabs, string shortlist, string precision)
    {
        var vocabLines = string.Join(Environment.NewLine, vocabs.Select(value => $"- {value}"));
        return $"""
            # CyCapture Bergamot config v2
            relative-paths: true
            models:
            - {model}
            vocabs:
            {vocabLines}
            shortlist:
            - {shortlist}
            - false
            beam-size: 1
            normalize: 1.0
            word-penalty: 0
            max-length-break: 128
            mini-batch-words: 1024
            workspace: 128
            max-length-factor: 2.0
            skip-cost: true
            cpu-threads: 0
            quiet: true
            quiet-translation: true
            gemm-precision: {precision}
            """;
    }

    private static string Direction(string source, string target) => $"{source}-{target}";
    private static string ConfigPath(string direction) => Path.Combine(TranslationDirectory, direction, "config.yml");
    private static bool IsConfigReady(string path) => File.Exists(path)
        && File.ReadAllText(path).Contains("# CyCapture Bergamot config v2", StringComparison.Ordinal);

    private static Exception UnsupportedPair(string source, string target) => new InvalidOperationException(
        UiLocalization.Text(
            $"Aucun modèle local n’est disponible pour {TranslationLanguageCatalog.DisplayName(source)} → {TranslationLanguageCatalog.DisplayName(target)}.",
            $"No local model is available for {TranslationLanguageCatalog.DisplayName(source)} → {TranslationLanguageCatalog.DisplayName(target)}."));

    private static void VerifySha256(string path, string expected)
    {
        using var stream = File.OpenRead(path);
        var actual = Convert.ToHexString(SHA256.HashData(stream));
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(path);
            throw new InvalidDataException($"L’empreinte du modèle {Path.GetFileName(path)} est incorrecte.");
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("CyCapture/1.4 (+https://github.com/MrMybal/CyCapture)");
        return client;
    }
}
