---
id: video
title: Capture vidéo
description: Enregistrez une zone ou un écran en MP4/H.264 avec du son facultatif.
section: Capture
parent: selection
order: 50
sourceRevision: 3
---

## Démarrer et arrêter {#recording}

Choisissez Vidéo, sélectionnez Intelligente, Fenêtre, Écran ou Zone libre, puis confirmez. Appuyez de nouveau sur **Impr. écran** ou utilisez **Arrêter l’enregistrement** dans le tray pour finaliser le MP4.

```diagram
{"type":"flow","title":"Pipeline vidéo","nodes":[{"id":"select","label":"Sélectionner","x":0,"y":50},{"id":"capture","label":"Vidéo + son facultatif","x":240,"y":50},{"id":"encode","label":"H.264","x":520,"y":50},{"id":"file","label":"MP4","x":740,"y":50}],"edges":[{"from":"select","to":"capture"},{"from":"capture","to":"encode"},{"from":"encode","to":"file"}]}
```

## Qualité et performances {#quality}

- Profils de fluidité : **24, 30 ou 60 i/s**.
- Profils H.264 : **1, 2,5, 6 ou 12 Mbit/s**.
- La fluidité et le débit sont indépendants.
- Windows Graphics Capture assure l’acquisition sur les systèmes Windows compatibles.

## Son de la vidéo {#audio}

Le son système et le microphone sont deux options Vidéo indépendantes. Sans source active, le MP4 est silencieux. Les profils vont de 96 kbit/s mono à 192 kbit/s stéréo.

## Cadre et presse-papiers {#frame}

Le cadre rouge fin et le minuteur facultatifs indiquent la zone active et sont exclus de la capture. Le cadre Vidéo se règle séparément du cadre GIF. Les vidéos terminées sont copiées par défaut dans le presse-papiers Windows comme pièces jointes ; ce comportement peut être désactivé dans l’onglet Vidéo.

Attendez la notification de finalisation avant d’ouvrir un long enregistrement.
