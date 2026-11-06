---
id: plugin-api
title: API de plugins
description: Contrat C# public pour les extensions de post-traitement CyCapture.
section: Extensibilité
parent: plugins
order: 150
sourceRevision: 2
---

## Contrat {#contract}

CyCapture découvre les DLL placées dans `%LOCALAPPDATA%\CyCapture\Plugins`. Un post-traitement implémente `ICapturePostProcessor` ; des interfaces facultatives ajoutent métadonnées, réglages générés, initialisation par l’hôte et préparation en arrière-plan.

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
        // Traiter uniquement les modes pris en charge.
        return ValueTask.CompletedTask;
    }
}
```

Les références API générées présentent les signatures publiques sans distribuer les corps d’implémentation. Commencez par [`ICapturePostProcessor`](doc:cycapture-api-icapturepostprocessor-icapturepostprocessor#signature) et [`ProcessAsync`](doc:cycapture-api-icapturepostprocessor-processasync-icapturepostprocessor-processasync#signature).

## Métadonnées facultatives {#metadata}

`ICapturePluginMetadata` expose une description, l’état actif par défaut, une étiquette d’accès rapide et des réglages `Text`, `Boolean` ou `Choice`. Une étiquette non vide crée une case synchronisée dans le tray et la palette.

`ICapturePluginHostAware.InitializeHost` reçoit le dossier de données du plugin. `ICapturePluginPreparable.PrepareAsync` permet une préparation en arrière-plan après le démarrage.

## Comportement de l’hôte {#host-behavior}

- L’état actif et les valeurs sont stockés dans les réglages CyCapture de l’utilisateur courant.
- `CaptureArtifact.Mode` indique Image, Video, Gif ou Audio ; un plugin ignore les modes incompatibles.
- Une erreur de chargement ou de configuration désactive seulement le plugin concerné et est journalisée dans Plugins.
- Une erreur de traitement ne supprime ni n’invalide la capture originale.
- Les processeurs actifs s’exécutent séquentiellement après la capture.

## Modèle de capture {#capture-model}

[`CaptureArtifact`](doc:cycapture-api-captureartifact-captureartifact#signature) fournit le chemin final, le [`CaptureMode`](doc:cycapture-api-capturemode-capturemode#signature), la sélection et les dates de début et de fin. [`CaptureSelection`](doc:cycapture-api-captureselection-captureselection#signature) contient les coordonnées du bureau virtuel et l’écran d’une capture visuelle.

## Intégration CyAnnota {#cyannota-integration}

L’intégration CyAnnota fournie est un post-processeur CyCapture isolé. Elle prépare l’application Windows intégrée, attend sa première fenêtre si nécessaire et lui transmet directement le chemin du média terminé. Elle n’utilise pas d’association `cyannota://` et ne modifie pas les sources de CyAnnota.
