---
id: storage
title: Dossiers, historique et métadonnées
description: Organisez les captures et choisissez le stockage des informations techniques.
section: Bibliothèque
parent: gallery
order: 100
sourceRevision: 2
---

## Organisation des dossiers {#folders}

Choisissez n’importe quel dossier absolu accessible en écriture dans les Réglages.

```diagram
{"type":"architecture","title":"Organisation facultative","nodes":[{"id":"root","label":"CyCapture","x":0,"y":100},{"id":"day","label":"AAAA-MM-JJ","x":240,"y":100},{"id":"images","label":"Images","x":500,"y":0},{"id":"videos","label":"Videos","x":500,"y":70},{"id":"gifs","label":"GIFs","x":500,"y":140},{"id":"audio","label":"Audio","x":500,"y":210}],"edges":[{"from":"root","to":"day"},{"from":"day","to":"images"},{"from":"day","to":"videos"},{"from":"day","to":"gifs"},{"from":"day","to":"audio"}]}
```

- **Dossiers quotidiens** crée un dossier `AAAA-MM-JJ` sous le dossier de capture.
- **Séparer par type** crée Images, Videos, GIFs et Audio sous le dossier du jour actif, ou directement sous la racine lorsque les dossiers quotidiens sont désactivés.
- Les deux options peuvent être combinées.

## Métadonnées de capture {#metadata}

Le plugin Manifeste de capture propose quatre modes :

| Mode | Résultat |
| --- | --- |
| Intégré au média | Place les informations dans PNG, JPEG, GIF ou MP4 lorsque le format le permet. |
| Base centrale | Conserve les informations dans les données locales de CyCapture. |
| JSON adjacent | Écrit un fichier `.cycapture.json` à côté de la capture. |
| Aucune métadonnée | Désactive la création du manifeste. |

## Historique {#history}

La Galerie référence les fichiers créés par CyCapture. Déplacer ou supprimer un fichier en dehors de CyCapture peut laisser une entrée indisponible jusqu’à l’actualisation.
