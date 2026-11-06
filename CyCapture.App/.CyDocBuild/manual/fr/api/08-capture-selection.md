---
id: cycapture-api-captureselection-captureselection
title: CaptureSelection
description: Décrit la cible et le bureau virtuel d’une capture visuelle.
section: API importée
order: 257
sourceRevision: 02a5c16f5fc4
---

Cible choisie pour une capture visuelle, avec coordonnées du bureau, écran et éventuelles annotations sur l’image figée.

## Signature {#signature}

```csharp
public sealed record CaptureSelection(PixelBounds Bounds, MonitorDescriptor Monitor, SelectionKind Kind, string Title, nint WindowHandle = 0, byte[]? FrozenFramePng = null, QuickAnnotationDocument? QuickAnnotations = null)
```
