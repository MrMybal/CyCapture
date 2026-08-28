using System.Diagnostics;
using CyCapture.Models;
using CyCapture.Services;

namespace CyCapture.Plugins;

internal sealed class CyAnnotaPostProcessor : ICapturePostProcessor, ICapturePluginMetadata
{
    private string _configuredExecutable = string.Empty;
    private string _launchMode = "Automatique";
    private bool _openImages = true;
    private bool _openVideos = true;

    public string Id => "cyannota-post-edit";
    public string Name => "CyAnnota Post Edit";
    public string Description => "Ouvre automatiquement les images ou vidéos terminées dans CyAnnota.";
    public bool EnabledByDefault => false;
    public string? QuickAccessLabel => "PostEdit with CyAnnota";
    public IReadOnlyList<CapturePluginSettingDefinition> Settings =>
    [
        new("executablePath", "Chemin de CyAnnota.exe", CapturePluginSettingKind.Text),
        new("launchMode", "Mode de lancement", CapturePluginSettingKind.Choice, "Automatique",
            ["Automatique", "Exécutable", "Protocole cyannota://"]),
        new("openImages", "Ouvrir les images dans CyAnnota", CapturePluginSettingKind.Boolean, "true"),
        new("openVideos", "Ouvrir les vidéos dans CyAnnota", CapturePluginSettingKind.Boolean, "true")
    ];

    public void ApplySettings(IReadOnlyDictionary<string, string> values)
    {
        _configuredExecutable = values.GetValueOrDefault("executablePath", string.Empty).Trim();
        _launchMode = values.GetValueOrDefault("launchMode", "Automatique");
        _openImages = ReadBoolean(values, "openImages", true);
        _openVideos = ReadBoolean(values, "openVideos", true);
    }

    public ValueTask ProcessAsync(CaptureArtifact artifact, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (artifact.Mode == CaptureMode.Gif) return ValueTask.CompletedTask;
        if (artifact.Mode == CaptureMode.Image && !_openImages) return ValueTask.CompletedTask;
        if (artifact.Mode == CaptureMode.Video && !_openVideos) return ValueTask.CompletedTask;
        if (!File.Exists(artifact.Path))
            throw new FileNotFoundException("La capture à ouvrir dans CyAnnota est introuvable.", artifact.Path);

        if (_launchMode.StartsWith("Protocole", StringComparison.OrdinalIgnoreCase))
        {
            StartProtocol(artifact.Path);
            return ValueTask.CompletedTask;
        }

        var executable = ResolveExecutable();
        if (executable is not null)
        {
            var startInfo = new ProcessStartInfo(executable)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(executable)!
            };
            startInfo.ArgumentList.Add(Path.GetFullPath(artifact.Path));
            if (Process.Start(startInfo) is null)
                throw new InvalidOperationException("Windows n’a pas pu démarrer CyAnnota.");
            return ValueTask.CompletedTask;
        }

        if (_launchMode.Equals("Exécutable", StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException("CyAnnota.exe est introuvable. Configurez son chemin dans les réglages du plugin.");

        StartProtocol(artifact.Path);
        return ValueTask.CompletedTask;
    }

    internal string? ResolveExecutable()
    {
        var processDirectory = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        var candidates = new List<string?>
        {
            _configuredExecutable,
            Environment.GetEnvironmentVariable("CYANNOTA_PATH"),
            Path.Combine(processDirectory, "CyAnnota.exe"),
            Path.Combine(AppContext.BaseDirectory, "CyAnnota.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "CyAnnota", "CyAnnota.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "CyAnnota", "CyAnnota.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "CyAnnota", "CyAnnota.exe")
        };
        var developmentRelease = FindDevelopmentReleaseDirectory();
        if (developmentRelease is not null && Directory.Exists(developmentRelease))
        {
            candidates.Add(Path.Combine(developmentRelease, "win-unpacked", "CyAnnota.exe"));
            candidates.AddRange(Directory.EnumerateFiles(developmentRelease, "CyAnnota-*-portable.exe")
                .OrderByDescending(File.GetLastWriteTimeUtc));
        }

        return candidates
            .Where(path => !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path))
            .Select(path => Path.GetFullPath(path!))
            .FirstOrDefault(File.Exists);
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

    private static void StartProtocol(string mediaPath)
    {
        var url = "cyannota://open?path=" + Uri.EscapeDataString(Path.GetFullPath(mediaPath));
        if (Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }) is null)
            throw new InvalidOperationException("Le protocole cyannota:// n’est pas disponible.");
    }

    private static bool ReadBoolean(IReadOnlyDictionary<string, string> values, string key, bool fallback) =>
        values.TryGetValue(key, out var text) && bool.TryParse(text, out var parsed) ? parsed : fallback;
}
