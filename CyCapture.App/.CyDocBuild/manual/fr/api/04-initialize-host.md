---
id: cycapture-api-icapturepluginhostaware-initializehost-icapturepluginhostaware-initializehost
title: ICapturePluginHostAware.InitializeHost
description: Reçoit le dossier de données CyCapture de l’utilisateur.
section: API importée
order: 253
sourceRevision: 037715223f9c
---

Rappel d’initialisation facultatif qui transmet au plugin le dossier de données CyCapture de l’utilisateur.

## Signature {#signature}

```csharp
void InitializeHost(string dataDirectory)
```

## Paramètres {#parameters}

| Nom | Type | Description |
| --- | --- | --- |
| `dataDirectory` | `string` | Chemin absolu du dossier de données CyCapture de l’utilisateur courant. |

## Retour {#returns}

`void` — Aucune valeur.
