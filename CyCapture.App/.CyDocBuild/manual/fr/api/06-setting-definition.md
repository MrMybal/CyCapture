---
id: cycapture-api-capturepluginsettingdefinition-capturepluginsettingdefinition
title: CapturePluginSettingDefinition
description: Décrit un réglage de plugin rendu automatiquement.
section: API importée
order: 255
sourceRevision: 1ef166a893d1
---

Décrit un réglage affiché automatiquement dans la fenêtre Réglages. Les types pris en charge sont `Text`, `Boolean` et `Choice`.

## Signature {#signature}

```csharp
public sealed record CapturePluginSettingDefinition(string Key, string Label, CapturePluginSettingKind Kind, string DefaultValue = "", IReadOnlyList<string>? Choices = null)
```

## Paramètres {#parameters}

| Nom | Type | Valeur par défaut | Description |
| --- | --- | --- | --- |
| `Key` | `string` |  | Clé de stockage stable. |
| `Label` | `string` |  | Libellé lisible du champ. |
| `Kind` | `CapturePluginSettingKind` |  | Type d’éditeur : Text, Boolean ou Choice. |
| `DefaultValue` | `string` | `""` | Valeur utilisée si aucun réglage utilisateur n’existe. |
| `Choices` | `IReadOnlyList<string>?` | `null` | Valeurs autorisées pour un champ Choice. |
