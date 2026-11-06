---
id: gif
title: Capture GIF
description: Créez directement une animation prête à partager depuis une zone sélectionnée.
section: Capture
parent: selection
order: 60
sourceRevision: 2
---

La capture GIF est silencieuse et possède ses propres profils de dimension et de fluidité.

## Profils de qualité {#quality}

| Profil | Fluidité | Côté long maximal |
| --- | ---: | ---: |
| Compact | 8 i/s | 720 px |
| Équilibré | 10 i/s | 960 px |
| Haute qualité | 12 i/s | 1280 px |

## Enregistrement {#recording}

Sélectionnez Intelligente, Fenêtre, Écran ou Zone libre comme pour une vidéo. Un cadre rouge propre au GIF peut être activé ou désactivé. Appuyez de nouveau sur Impr. écran pour arrêter et laisser CyCapture encoder l’animation.

```diagram
{"type":"flow","title":"Pipeline GIF","nodes":[{"id":"select","label":"Sélectionner","x":0,"y":50},{"id":"frames","label":"Capturer les images","x":240,"y":50},{"id":"optimize","label":"Redimensionner et optimiser","x":490,"y":50},{"id":"gif","label":"GIF","x":750,"y":50}],"edges":[{"from":"select","to":"frames"},{"from":"frames","to":"optimize"},{"from":"optimize","to":"gif"}]}
```

```callout
{"type":"note","title":"Aucun son","body":"La capture GIF n’enregistre jamais le son système ou le microphone."}
```
