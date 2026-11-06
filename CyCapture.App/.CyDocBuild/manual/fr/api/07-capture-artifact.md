---
id: cycapture-api-captureartifact-captureartifact
title: CaptureArtifact
description: Décrit une capture finalisée transmise aux plugins.
section: API importée
order: 256
sourceRevision: 1afdbb7f167c
---

Description immuable d’une capture image, vidéo, GIF ou audio finalisée et transmise aux post-processeurs.

## Signature {#signature}

```csharp
public sealed record CaptureArtifact(string Path, CaptureMode Mode, CaptureSelection Selection, DateTimeOffset StartedAt, DateTimeOffset FinishedAt)
```

## Paramètres {#parameters}

| Nom | Type | Description |
| --- | --- | --- |
| `Path` | `string` | Chemin absolu du média terminé. |
| `Mode` | `CaptureMode` | Image, Video, Gif ou Audio. |
| `Selection` | `CaptureSelection` | Sélection visuelle ; l’audio utilise un descripteur vide. |
| `StartedAt` | `DateTimeOffset` | Date et heure de début. |
| `FinishedAt` | `DateTimeOffset` | Date et heure de fin. |

## Références {#references}

[CaptureMode](doc:cycapture-api-capturemode-capturemode), [CaptureSelection](doc:cycapture-api-captureselection-captureselection)
