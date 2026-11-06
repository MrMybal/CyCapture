---
id: plugin-api
title: Plugin API
description: Public C# contract for CyCapture post-processing extensions.
section: Extensibility
parent: plugins
order: 150
revision: 2
---

## Contract {#contract}

CyCapture discovers DLLs placed in `%LOCALAPPDATA%\CyCapture\Plugins`. A post-processor implements `ICapturePostProcessor`; optional interfaces add metadata, generated settings, host initialization and background preparation.

```csharp
using CyCapture.Models;
using CyCapture.Services;

public sealed class ExamplePostProcessor : ICapturePostProcessor
{
    public string Id => "example-post-edit";
    public string Name => "Example Post Edit";

    public ValueTask ProcessAsync(
        CaptureArtifact artifact,
        CancellationToken cancellationToken)
    {
        // Process only supported artifact modes.
        return ValueTask.CompletedTask;
    }
}
```

The generated API references document public signatures without distributing implementation bodies. Start with [`ICapturePostProcessor`](doc:cycapture-api-icapturepostprocessor-icapturepostprocessor#signature) and [`ProcessAsync`](doc:cycapture-api-icapturepostprocessor-processasync-icapturepostprocessor-processasync#signature).

## Optional metadata {#metadata}

`ICapturePluginMetadata` exposes a description, default enabled state, quick-access label and `Text`, `Boolean` or `Choice` settings. A non-empty quick-access label creates a synchronized toggle in the tray and quick palette.

`ICapturePluginHostAware.InitializeHost` receives the plugin data directory. `ICapturePluginPreparable.PrepareAsync` lets a plugin perform background preparation after startup.

## Host behavior {#host-behavior}

- Enabled state and values are stored in the current user's CyCapture settings.
- `CaptureArtifact.Mode` is Image, Video, Gif or Audio; a plugin should ignore unsupported modes.
- Load/configuration failures disable only the affected plugin and are logged in the Plugins folder.
- Processing failures do not delete or invalidate the original capture.
- Enabled processors run sequentially after capture completion.

## Capture model {#capture-model}

[`CaptureArtifact`](doc:cycapture-api-captureartifact-captureartifact#signature) provides the completed file path, [`CaptureMode`](doc:cycapture-api-capturemode-capturemode#signature), selection information and start/finish timestamps. [`CaptureSelection`](doc:cycapture-api-captureselection-captureselection#signature) contains the virtual-desktop bounds and monitor used for visual captures.

## CyAnnota integration {#cyannota-integration}

The bundled CyAnnota integration is an isolated CyCapture post-processor. It prepares the bundled Windows application, waits for its first window when necessary and passes the completed media path directly. It does not use a `cyannota://` association or modify CyAnnota source files.
