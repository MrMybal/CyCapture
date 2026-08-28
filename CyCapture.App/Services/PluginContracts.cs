namespace CyCapture.Services;

public enum CapturePluginSettingKind
{
    Text,
    Boolean,
    Choice
}

public sealed record CapturePluginSettingDefinition(
    string Key,
    string Label,
    CapturePluginSettingKind Kind,
    string DefaultValue = "",
    IReadOnlyList<string>? Choices = null);

public interface ICapturePluginMetadata
{
    string Description { get; }
    bool EnabledByDefault => true;
    string? QuickAccessLabel { get; }
    IReadOnlyList<CapturePluginSettingDefinition> Settings { get; }
    void ApplySettings(IReadOnlyDictionary<string, string> values);
}

internal interface ICapturePluginHostAware
{
    void InitializeHost(string dataDirectory);
}

internal sealed record CapturePluginDescriptor(
    string Id,
    string Name,
    string Description,
    bool Enabled,
    string? QuickAccessLabel,
    IReadOnlyList<CapturePluginSettingDefinition> Settings,
    IReadOnlyDictionary<string, string> Values);

internal sealed class StoredPluginState
{
    public bool Enabled { get; set; } = true;
    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
