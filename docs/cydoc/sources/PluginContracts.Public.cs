// Distribution-safe snapshot of the public contracts from CyCapture.Services.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CyCapture.Models;

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

public interface ICapturePostProcessor
{
    string Id { get; }
    string Name { get; }
    ValueTask ProcessAsync(CaptureArtifact artifact, CancellationToken cancellationToken);
}

public interface ICapturePluginMetadata
{
    string Description { get; }
    bool EnabledByDefault => true;
    string? QuickAccessLabel { get; }
    IReadOnlyList<CapturePluginSettingDefinition> Settings { get; }
    void ApplySettings(IReadOnlyDictionary<string, string> values);
}

public interface ICapturePluginHostAware
{
    void InitializeHost(string dataDirectory);
}

public interface ICapturePluginPreparable
{
    ValueTask PrepareAsync(CancellationToken cancellationToken);
}
