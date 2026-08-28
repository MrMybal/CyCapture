using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using CyCapture.Models;
using CyCapture.Plugins;

namespace CyCapture.Services;

public interface ICapturePostProcessor
{
    string Id { get; }
    string Name { get; }
    ValueTask ProcessAsync(CaptureArtifact artifact, CancellationToken cancellationToken);
}

internal sealed class PostProcessingService
{
    private readonly List<ICapturePostProcessor> _processors = [new ManifestPostProcessor(), new CyAnnotaPostProcessor()];
    private readonly Dictionary<string, StoredPluginState> _states;
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private readonly object _sync = new();
    private readonly string _dataDirectory;

    internal PostProcessingService(
        string? dataDirectory = null,
        bool loadExternalPlugins = true,
        IEnumerable<ICapturePostProcessor>? additionalProcessors = null)
    {
        _dataDirectory = dataDirectory ?? Preferences.DataDirectory;
        _states = LoadStates();
        if (loadExternalPlugins) LoadExternalPlugins();
        foreach (var processor in additionalProcessors ?? [])
        {
            if (!_processors.Any(item => item.Id.Equals(processor.Id, StringComparison.OrdinalIgnoreCase)))
                _processors.Add(processor);
        }
        InitializePluginStates();
    }

    internal string PluginsDirectory => Path.Combine(_dataDirectory, "Plugins");
    private string StatePath => Path.Combine(_dataDirectory, "plugin-settings.json");

    internal event EventHandler? PluginsChanged;

    internal async Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        List<(ICapturePostProcessor Processor, ICapturePluginPreparable Preparable)> preparable;
        lock (_sync)
            preparable = _processors.OfType<ICapturePluginPreparable>()
                .Select(item => ((ICapturePostProcessor)item, item))
                .ToList();

        foreach (var (processor, item) in preparable)
        {
            try
            {
                await item.PrepareAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception error)
            {
                Directory.CreateDirectory(PluginsDirectory);
                await File.AppendAllTextAsync(
                    Path.Combine(PluginsDirectory, "plugin-errors.log"),
                    $"{DateTimeOffset.Now:O} · {processor.Id} · préparation · {error}{Environment.NewLine}");
            }
        }
    }

    internal IReadOnlyList<CapturePluginDescriptor> GetPlugins()
    {
        lock (_sync)
        {
            return _processors.Select(processor =>
            {
                var metadata = processor as ICapturePluginMetadata;
                var state = _states[processor.Id];
                return new CapturePluginDescriptor(
                    processor.Id,
                    processor.Name,
                    metadata?.Description ?? "Post-traitement C#",
                    state.Enabled,
                    metadata?.QuickAccessLabel,
                    metadata?.Settings ?? [],
                    new Dictionary<string, string>(state.Values, StringComparer.OrdinalIgnoreCase));
            }).ToList();
        }
    }

    internal async Task SetEnabledAsync(string id, bool enabled)
    {
        lock (_sync)
        {
            if (!_states.TryGetValue(id, out var state)) return;
            state.Enabled = enabled;
        }
        await SaveStatesAsync();
        PluginsChanged?.Invoke(this, EventArgs.Empty);
    }

    internal async Task SetSettingAsync(string id, string key, string value)
    {
        ICapturePostProcessor? processor;
        lock (_sync)
        {
            processor = _processors.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (processor is not ICapturePluginMetadata metadata || !_states.TryGetValue(id, out var state)) return;
            var definition = metadata.Settings.FirstOrDefault(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (definition is null) return;
            state.Values[definition.Key] = NormalizeValue(definition, value);
            ApplySettings(processor, state);
        }
        await SaveStatesAsync();
        PluginsChanged?.Invoke(this, EventArgs.Empty);
    }

    internal async Task RunAsync(CaptureArtifact artifact, CancellationToken cancellationToken = default)
    {
        List<ICapturePostProcessor> enabled;
        lock (_sync)
            enabled = _processors.Where(processor => _states.TryGetValue(processor.Id, out var state) && state.Enabled).ToList();

        foreach (var processor in enabled)
        {
            try
            {
                await processor.ProcessAsync(artifact, cancellationToken);
            }
            catch (Exception error)
            {
                await AppendPluginErrorAsync(processor, artifact, error);
            }
        }
    }

    private Dictionary<string, StoredPluginState> LoadStates()
    {
        try
        {
            if (!File.Exists(StatePath)) return new Dictionary<string, StoredPluginState>(StringComparer.OrdinalIgnoreCase);
            var json = File.ReadAllText(StatePath);
            var values = JsonSerializer.Deserialize<Dictionary<string, StoredPluginState>>(json)
                         ?? new Dictionary<string, StoredPluginState>();
            return new Dictionary<string, StoredPluginState>(values, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, StoredPluginState>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void InitializePluginStates()
    {
        lock (_sync)
        {
            foreach (var processor in _processors)
            {
                if (!_states.TryGetValue(processor.Id, out var state))
                {
                    state = new StoredPluginState
                    {
                        Enabled = (processor as ICapturePluginMetadata)?.EnabledByDefault ?? true
                    };
                    _states[processor.Id] = state;
                }
                if (processor is ICapturePluginHostAware hostAware)
                    hostAware.InitializeHost(_dataDirectory);
                if (processor is ICapturePluginMetadata metadata)
                {
                    foreach (var setting in metadata.Settings)
                    {
                        var storedValue = state.Values.GetValueOrDefault(setting.Key, setting.DefaultValue);
                        state.Values[setting.Key] = NormalizeValue(setting, storedValue);
                    }
                    ApplySettings(processor, state);
                }
            }
        }
    }

    private void ApplySettings(ICapturePostProcessor processor, StoredPluginState state)
    {
        if (processor is not ICapturePluginMetadata metadata) return;
        try
        {
            metadata.ApplySettings(new Dictionary<string, string>(state.Values, StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception error)
        {
            state.Enabled = false;
            Directory.CreateDirectory(PluginsDirectory);
            File.AppendAllText(
                Path.Combine(PluginsDirectory, "plugin-errors.log"),
                $"{DateTimeOffset.Now:O} · {processor.Id} · configuration · {error}{Environment.NewLine}");
        }
    }

    private async Task SaveStatesAsync()
    {
        await _saveGate.WaitAsync();
        try
        {
            Dictionary<string, StoredPluginState> snapshot;
            lock (_sync)
            {
                snapshot = _states.ToDictionary(
                    item => item.Key,
                    item => new StoredPluginState
                    {
                        Enabled = item.Value.Enabled,
                        Values = new Dictionary<string, string>(item.Value.Values, StringComparer.OrdinalIgnoreCase)
                    },
                    StringComparer.OrdinalIgnoreCase);
            }
            Directory.CreateDirectory(_dataDirectory);
            await using var stream = File.Create(StatePath);
            await JsonSerializer.SerializeAsync(stream, snapshot, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception error)
        {
            Directory.CreateDirectory(PluginsDirectory);
            File.AppendAllText(
                Path.Combine(PluginsDirectory, "plugin-errors.log"),
                $"{DateTimeOffset.Now:O} · état des plugins · {error}{Environment.NewLine}");
        }
        finally
        {
            _saveGate.Release();
        }
    }

    private static string NormalizeValue(CapturePluginSettingDefinition definition, string value) => definition.Kind switch
    {
        CapturePluginSettingKind.Boolean => bool.TryParse(value, out var enabled) && enabled ? "true" : "false",
        CapturePluginSettingKind.Choice when definition.Choices?.Contains(value, StringComparer.OrdinalIgnoreCase) == true =>
            definition.Choices.First(choice => choice.Equals(value, StringComparison.OrdinalIgnoreCase)),
        CapturePluginSettingKind.Choice => definition.DefaultValue,
        _ => value
    };

    private void LoadExternalPlugins()
    {
        Directory.CreateDirectory(PluginsDirectory);
        foreach (var path in Directory.EnumerateFiles(PluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var context = new AssemblyLoadContext($"CyCapture.Plugin.{Path.GetFileNameWithoutExtension(path)}", false);
                var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(path));
                foreach (var type in assembly.GetTypes().Where(type =>
                             !type.IsAbstract && typeof(ICapturePostProcessor).IsAssignableFrom(type)))
                {
                    if (Activator.CreateInstance(type) is ICapturePostProcessor plugin
                        && !_processors.Any(item => item.Id.Equals(plugin.Id, StringComparison.OrdinalIgnoreCase)))
                        _processors.Add(plugin);
                }
            }
            catch (Exception error)
            {
                File.AppendAllText(Path.Combine(PluginsDirectory, "plugin-errors.log"),
                    $"{DateTimeOffset.Now:O} · {path} · {error}\n");
            }
        }
    }

    private static Task AppendPluginErrorAsync(ICapturePostProcessor processor, CaptureArtifact artifact, Exception error)
    {
        var path = Path.Combine(Path.GetDirectoryName(artifact.Path)!, "cycapture-plugin-errors.log");
        return File.AppendAllTextAsync(path,
            $"{DateTimeOffset.Now:O} · {processor.Id} · {Path.GetFileName(artifact.Path)} · {error.Message}\n");
    }
}

internal sealed class ManifestPostProcessor : ICapturePostProcessor, ICapturePluginMetadata, ICapturePluginHostAware
{
    private bool _includeSelection = true;
    private bool _includeTiming = true;
    private string _storage = "Intégré au média";
    private string _dataDirectory = Preferences.DataDirectory;

    public string Id => "manifest-sidecar";
    public string Name => "Manifeste de capture";
    public string Description => "Stocke les métadonnées dans le média, dans une base centrale ou dans un JSON adjacent.";
    public bool EnabledByDefault => true;
    public string? QuickAccessLabel => null;
    public IReadOnlyList<CapturePluginSettingDefinition> Settings =>
    [
        new("storage", "Stockage des métadonnées", CapturePluginSettingKind.Choice, "Intégré au média",
            ["Intégré au média", "Base centrale", "JSON adjacent", "Aucune métadonnée"]),
        new("includeSelection", "Inclure les détails de sélection", CapturePluginSettingKind.Boolean, "true"),
        new("includeTiming", "Inclure les informations de durée", CapturePluginSettingKind.Boolean, "true")
    ];

    public void ApplySettings(IReadOnlyDictionary<string, string> values)
    {
        _storage = values.GetValueOrDefault("storage", "Intégré au média");
        _includeSelection = !values.TryGetValue("includeSelection", out var selection) || !bool.TryParse(selection, out var parsedSelection) || parsedSelection;
        _includeTiming = !values.TryGetValue("includeTiming", out var timing) || !bool.TryParse(timing, out var parsedTiming) || parsedTiming;
    }

    public void InitializeHost(string dataDirectory) => _dataDirectory = dataDirectory;

    public async ValueTask ProcessAsync(CaptureArtifact artifact, CancellationToken cancellationToken)
    {
        var info = new FileInfo(artifact.Path);
        var manifest = new Dictionary<string, object?>
        {
            ["schema"] = "cycapture/1",
            ["mediaType"] = artifact.Mode.ToString().ToLowerInvariant(),
            ["path"] = artifact.Path,
            ["bytes"] = info.Exists ? info.Length : 0
        };
        if (_includeTiming)
        {
            manifest["startedAt"] = artifact.StartedAt;
            manifest["finishedAt"] = artifact.FinishedAt;
            manifest["durationMs"] = Math.Max(0, (artifact.FinishedAt - artifact.StartedAt).TotalMilliseconds);
        }
        if (_includeSelection)
        {
            manifest["selection"] = new
            {
                kind = artifact.Selection.Kind.ToString().ToLowerInvariant(),
                artifact.Selection.Title,
                x = artifact.Selection.Bounds.X,
                y = artifact.Selection.Bounds.Y,
                width = artifact.Selection.Bounds.Width,
                height = artifact.Selection.Bounds.Height,
                display = artifact.Selection.Monitor.DeviceName
            };
        }
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        await CaptureMetadataWriter.WriteAsync(artifact.Path, json, _storage, _dataDirectory, cancellationToken);
    }
}
