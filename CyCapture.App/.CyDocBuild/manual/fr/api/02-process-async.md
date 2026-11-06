---
id: cycapture-api-icapturepostprocessor-processasync-icapturepostprocessor-processasync
title: ICapturePostProcessor.ProcessAsync
description: Traite une capture une fois son fichier finalisé.
section: API importée
order: 251
sourceRevision: f6c68701ee22
---

S’exécute après la finalisation du fichier. Un plugin doit laisser l’original utilisable en cas d’échec et respecter l’annulation lorsque cela est possible.

## Signature {#signature}

```csharp
ValueTask ProcessAsync(CaptureArtifact artifact, CancellationToken cancellationToken)
```

## Paramètres {#parameters}

| Nom | Type | Description |
| --- | --- | --- |
| `artifact` | `CaptureArtifact` | Capture terminée et métadonnées de sélection. |
| `cancellationToken` | `CancellationToken` | Jeton signalé lorsque CyCapture s’arrête ou annule le pipeline. |

## Retour {#returns}

`ValueTask` — Tâche terminée quand le post-traitement est fini.

## Références {#references}

[CaptureArtifact](doc:cycapture-api-captureartifact-captureartifact)
