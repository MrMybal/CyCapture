using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using CyCapture.Models;
using CyCapture.Services;

namespace CyCapture.Plugins;

internal sealed class CyAnnotaPostProcessor :
    ICapturePostProcessor,
    ICapturePluginMetadata,
    ICapturePluginHostAware,
    ICapturePluginPreparable
{
    internal const string BundledVersion = "0.3.6";
    internal const string BundledPortableFileName = $"CyAnnota-{BundledVersion}-portable.exe";
    internal const string BundledArchiveFileName = $"CyAnnota-{BundledVersion}-win-x64.zip";
    internal const string BundledPortableSha256 = "60A07568B71EB98F2F86EBA8869F61392548BDA7EC3FFD78274245351E4A93F2";
    private const long BundledPortableLength = 107_917_573;
    private const string BundledPortableResourceName = $"CyCapture.Bundled.{BundledPortableFileName}";
    private const string BundledArchiveResourceName = $"CyCapture.Bundled.{BundledArchiveFileName}";
    private static readonly object BundleSync = new();

    private readonly object _preparationSync = new();
    private readonly SemaphoreSlim _launchGate = new(1, 1);
    private string _configuredExecutable = string.Empty;
    private string _launchMode = "CyAnnota intégré";
    private string _dataDirectory = Preferences.DataDirectory;
    private string? _bundledExecutable;
    private Task<string?>? _preparationTask;
    private bool _openImages = true;
    private bool _openVideos = true;

    public string Id => "cyannota-post-edit";
    public string Name => "CyAnnota Post Edit";
    public string Description => $"Ouvre les images ou vidéos terminées avec CyAnnota {BundledVersion}, préchargé par CyCapture.";
    public bool EnabledByDefault => false;
    public string? QuickAccessLabel => "PostEdit with CyAnnota";
    public IReadOnlyList<CapturePluginSettingDefinition> Settings =>
    [
        new("executablePath", "Chemin d’un CyAnnota.exe personnalisé", CapturePluginSettingKind.Text),
        new("launchMode", "Version de CyAnnota", CapturePluginSettingKind.Choice, "CyAnnota intégré",
            ["CyAnnota intégré", "Exécutable personnalisé"]),
        new("openImages", "Ouvrir les images dans CyAnnota", CapturePluginSettingKind.Boolean, "true"),
        new("openVideos", "Ouvrir les vidéos dans CyAnnota", CapturePluginSettingKind.Boolean, "true")
    ];

    internal static bool HasBundledExecutable
    {
        get
        {
            var assembly = typeof(CyAnnotaPostProcessor).Assembly;
            return assembly.GetManifestResourceInfo(BundledArchiveResourceName) is not null
                   || assembly.GetManifestResourceInfo(BundledPortableResourceName) is not null;
        }
    }

    public void InitializeHost(string dataDirectory) => _dataDirectory = dataDirectory;

    public void ApplySettings(IReadOnlyDictionary<string, string> values)
    {
        _configuredExecutable = values.GetValueOrDefault("executablePath", string.Empty).Trim();
        var requestedMode = values.GetValueOrDefault("launchMode", "CyAnnota intégré");
        _launchMode = requestedMode.StartsWith("Exécutable", StringComparison.OrdinalIgnoreCase)
            ? "Exécutable personnalisé"
            : "CyAnnota intégré";
        _openImages = ReadBoolean(values, "openImages", true);
        _openVideos = ReadBoolean(values, "openVideos", true);
    }

    public ValueTask PrepareAsync(CancellationToken cancellationToken)
    {
        Task<string?> task;
        lock (_preparationSync)
            task = _preparationTask ??= Task.Run(ResolveBundledExecutable, CancellationToken.None);
        return new ValueTask(task.WaitAsync(cancellationToken));
    }

    public async ValueTask ProcessAsync(CaptureArtifact artifact, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (artifact.Mode == CaptureMode.Gif) return;
        if (artifact.Mode == CaptureMode.Image && !_openImages) return;
        if (artifact.Mode == CaptureMode.Video && !_openVideos) return;
        if (!File.Exists(artifact.Path))
            throw new FileNotFoundException("La capture à ouvrir dans CyAnnota est introuvable.", artifact.Path);

        var useIntegrated = !_launchMode.StartsWith("Exécutable", StringComparison.OrdinalIgnoreCase);
        if (useIntegrated) await PrepareAsync(cancellationToken);
        var executable = ResolveExecutable();
        if (executable is null)
        {
            var message = useIntegrated
                ? "La copie de CyAnnota intégrée à CyCapture est indisponible. Réinstallez la dernière version de CyCapture."
                : "L’exécutable CyAnnota personnalisé est introuvable. Vérifiez son chemin dans les réglages du plugin.";
            throw new FileNotFoundException(message);
        }

        if (useIntegrated)
            await OpenWithIntegratedCyAnnotaAsync(executable, artifact.Path, cancellationToken);
        else
            StartProcess(executable, artifact.Path)?.Dispose();
    }

    internal string? ResolveExecutable()
    {
        var configured = ExistingFile(_configuredExecutable);
        if (_launchMode.StartsWith("Exécutable", StringComparison.OrdinalIgnoreCase))
            return configured;

        var bundled = ResolveBundledExecutable();
        if (bundled is not null) return bundled;

        var processDirectory = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        var candidates = new List<string?>
        {
            configured,
            Environment.GetEnvironmentVariable("CYANNOTA_PATH"),
            Path.Combine(processDirectory, "CyAnnota.exe"),
            Path.Combine(AppContext.BaseDirectory, "CyAnnota.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "CyAnnota", "CyAnnota.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "CyAnnota", "CyAnnota.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "CyAnnota", "CyAnnota.exe")
        };
        candidates.AddRange(EnumeratePortableExecutables(processDirectory));
        if (!processDirectory.Equals(AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase))
            candidates.AddRange(EnumeratePortableExecutables(AppContext.BaseDirectory));

        var developmentRelease = FindDevelopmentReleaseDirectory();
        if (developmentRelease is not null && Directory.Exists(developmentRelease))
        {
            candidates.Add(Path.Combine(developmentRelease, "win-unpacked", "CyAnnota.exe"));
            candidates.AddRange(EnumeratePortableExecutables(developmentRelease));
        }

        return candidates.Select(ExistingFile).FirstOrDefault(path => path is not null);
    }

    private async Task OpenWithIntegratedCyAnnotaAsync(string executable, string mediaPath, CancellationToken cancellationToken)
    {
        await _launchGate.WaitAsync(cancellationToken);
        try
        {
            if (!IsCyAnnotaRunning())
            {
                using var launcher = StartProcess(executable, null)
                                     ?? throw new InvalidOperationException("Windows n’a pas pu démarrer CyAnnota.");
            }

            if (!IsCyAnnotaReady())
                await WaitUntilCyAnnotaReadyAsync(cancellationToken);

            using var handoff = StartProcess(executable, mediaPath)
                                ?? throw new InvalidOperationException("Windows n’a pas pu transmettre le média à CyAnnota.");
        }
        finally
        {
            _launchGate.Release();
        }
    }

    private static Process? StartProcess(string executable, string? mediaPath)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(executable)!
        };
        if (!string.IsNullOrWhiteSpace(mediaPath))
            startInfo.ArgumentList.Add(Path.GetFullPath(mediaPath));
        return Process.Start(startInfo);
    }

    private static bool IsCyAnnotaRunning()
    {
        foreach (var process in Process.GetProcessesByName("CyAnnota"))
        {
            try
            {
                if (!process.HasExited) return true;
            }
            catch
            {
                // Le processus peut se terminer entre l’énumération et la lecture de son état.
            }
            finally
            {
                process.Dispose();
            }
        }
        return false;
    }

    private static bool IsCyAnnotaReady()
    {
        foreach (var process in Process.GetProcessesByName("CyAnnota"))
        {
            try
            {
                if (process.HasExited) continue;
                process.Refresh();
                if (process.MainWindowHandle != 0) return true;
            }
            catch
            {
                // Le processus peut se terminer pendant le contrôle de sa fenêtre.
            }
            finally
            {
                process.Dispose();
            }
        }
        return false;
    }

    private static async Task WaitUntilCyAnnotaReadyAsync(CancellationToken cancellationToken)
    {
        var timeout = Stopwatch.StartNew();
        while (timeout.Elapsed < TimeSpan.FromSeconds(15))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsCyAnnotaReady())
            {
                await Task.Delay(300, cancellationToken);
                return;
            }
            await Task.Delay(100, cancellationToken);
        }
    }

    private string? ResolveBundledExecutable()
    {
        if (_bundledExecutable is not null && File.Exists(_bundledExecutable)) return _bundledExecutable;

        var assembly = typeof(CyAnnotaPostProcessor).Assembly;
        using var archive = assembly.GetManifestResourceStream(BundledArchiveResourceName);
        if (archive is not null) return ExtractBundledArchive(archive);

        using var portable = assembly.GetManifestResourceStream(BundledPortableResourceName);
        return portable is null ? null : ExtractBundledPortable(portable);
    }

    private string ExtractBundledArchive(Stream archive)
    {
        lock (BundleSync)
        {
            var destinationDirectory = Path.Combine(_dataDirectory, "Bundled", "CyAnnota", BundledVersion, "win-x64");
            var destinationPath = Path.Combine(destinationDirectory, "CyAnnota.exe");
            if (IsValidUnpackedDistribution(destinationDirectory))
            {
                _bundledExecutable = destinationPath;
                return destinationPath;
            }

            var parentDirectory = Path.GetDirectoryName(destinationDirectory)!;
            Directory.CreateDirectory(parentDirectory);
            var temporaryDirectory = destinationDirectory + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(temporaryDirectory);
                ZipFile.ExtractToDirectory(archive, temporaryDirectory, true);
                var extractedExecutable = Directory.EnumerateFiles(temporaryDirectory, "CyAnnota.exe", SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (extractedExecutable is null)
                    throw new InvalidDataException("L’archive CyAnnota intégrée ne contient pas CyAnnota.exe.");

                var extractedRoot = Path.GetDirectoryName(extractedExecutable)!;
                if (!IsValidUnpackedDistribution(extractedRoot))
                    throw new InvalidDataException("La distribution CyAnnota intégrée est incomplète.");

                if (Directory.Exists(destinationDirectory)) Directory.Delete(destinationDirectory, true);
                Directory.Move(extractedRoot, destinationDirectory);
                if (Directory.Exists(temporaryDirectory)) Directory.Delete(temporaryDirectory, true);
                _bundledExecutable = destinationPath;
                return destinationPath;
            }
            finally
            {
                if (Directory.Exists(temporaryDirectory)) Directory.Delete(temporaryDirectory, true);
            }
        }
    }

    private string ExtractBundledPortable(Stream resource)
    {
        lock (BundleSync)
        {
            var destinationDirectory = Path.Combine(_dataDirectory, "Bundled", "CyAnnota", BundledVersion);
            var destinationPath = Path.Combine(destinationDirectory, BundledPortableFileName);
            if (File.Exists(destinationPath) && new FileInfo(destinationPath).Length == BundledPortableLength)
            {
                _bundledExecutable = destinationPath;
                return destinationPath;
            }

            Directory.CreateDirectory(destinationDirectory);
            var temporaryPath = destinationPath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    resource.CopyTo(output);
                    output.Flush(true);
                }

                string hash;
                using (var extracted = File.OpenRead(temporaryPath))
                    hash = Convert.ToHexString(SHA256.HashData(extracted));
                if (!hash.Equals(BundledPortableSha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("La copie intégrée de CyAnnota n’a pas l’empreinte attendue.");

                File.Move(temporaryPath, destinationPath, true);
                _bundledExecutable = destinationPath;
                return destinationPath;
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }
    }

    private static bool IsValidUnpackedDistribution(string directory) =>
        File.Exists(Path.Combine(directory, "CyAnnota.exe"))
        && File.Exists(Path.Combine(directory, "resources.pak"))
        && File.Exists(Path.Combine(directory, "resources", "app.asar"));

    private static IEnumerable<string> EnumeratePortableExecutables(string directory)
    {
        if (!Directory.Exists(directory)) return [];
        return Directory.EnumerateFiles(directory, "CyAnnota-*-portable.exe", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc);
    }

    private static string? ExistingFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path)) return null;
        var fullPath = Path.GetFullPath(path);
        return File.Exists(fullPath) ? fullPath : null;
    }

    private static string? FindDevelopmentReleaseDirectory()
    {
        var roots = new[]
        {
            Path.GetDirectoryName(Environment.ProcessPath),
            AppContext.BaseDirectory
        };
        foreach (var root in roots.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            for (var directory = new DirectoryInfo(root!); directory is not null; directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, "CyAnnota_Project", "release");
                if (Directory.Exists(candidate)) return candidate;
            }
        }
        return null;
    }

    private static bool ReadBoolean(IReadOnlyDictionary<string, string> values, string key, bool fallback) =>
        values.TryGetValue(key, out var text) && bool.TryParse(text, out var parsed) ? parsed : fallback;
}
