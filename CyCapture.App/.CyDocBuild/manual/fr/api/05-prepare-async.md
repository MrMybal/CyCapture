---
id: cycapture-api-icapturepluginpreparable-prepareasync-icapturepluginpreparable-prepareasync
title: ICapturePluginPreparable.PrepareAsync
description: Prépare facultativement un plugin en arrière-plan.
section: API importée
order: 254
sourceRevision: 553c6512b89f
---

Préparation facultative en arrière-plan après le démarrage de CyCapture, avant l’envoi de la première capture terminée au plugin.

## Signature {#signature}

```csharp
ValueTask PrepareAsync(CancellationToken cancellationToken)
```

## Paramètres {#parameters}

| Nom | Type | Description |
| --- | --- | --- |
| `cancellationToken` | `CancellationToken` | Jeton signalé lorsque l’arrêt de l’application annule la préparation. |

## Retour {#returns}

`ValueTask` — Tâche terminée quand le plugin est prêt.
