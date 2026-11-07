---
id: print-screen
title: Comportement d’Impr. écran
description: Choisissez la palette, un mode direct ou le geste appui court et maintien.
section: Démarrage
parent: getting-started
order: 20
sourceRevision: 2
---

## Comportements disponibles {#behaviors}

- **Afficher le choix :** montre toujours Image, Vidéo, GIF, Audio, Texte et Traduction près du pointeur.
- **Image, Vidéo, GIF, Audio, Texte ou Traduction directe :** ignore la palette et lance immédiatement ce parcours.
- **Hybride :** un appui court ouvre la [[Sélection intelligente]] d’image ; un maintien ouvre la palette.

```diagram
{"type":"flow","title":"Geste hybride d’Impr. écran","nodes":[{"id":"key","label":"Impr. écran","x":0,"y":70},{"id":"tap","label":"Appui court","x":240,"y":0},{"id":"hold","label":"Maintien","x":240,"y":140},{"id":"image","label":"Sélection intelligente","x":500,"y":0},{"id":"palette","label":"Palette des modes","x":500,"y":140}],"edges":[{"from":"key","to":"tap"},{"from":"key","to":"hold"},{"from":"tap","to":"image"},{"from":"hold","to":"palette"}]}
```

## Délai du maintien {#hold-delay}

Le seuil hybride est réglable de **0,1 à 1,0 seconde**. Un délai court ouvre la palette plus vite ; un délai long réduit les maintiens involontaires.

## Règles contextuelles {#context-rules}

- Pendant une Vidéo, un GIF ou un Audio, Impr. écran arrête l’enregistrement.
- Si la sélection est ouverte, Impr. écran l’annule.
- La touche globale reste active tant que CyCapture fonctionne dans le tray.
- Les mêmes actions sont disponibles dans le menu du tray.

Consultez les [Réglages](doc:settings#print-screen) pour la configuration.
