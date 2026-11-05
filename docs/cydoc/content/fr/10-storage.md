---
id: storage
title: Dossiers, historique et métadonnées
description: Organisez les captures et choisissez le stockage des informations techniques.
section: Bibliothèque
order: 100
sourceRevision: 1
---

## Organisation des dossiers {#folders}

Choisissez n’importe quel dossier absolu accessible en écriture dans les Réglages.

```diagram
{"type":"architecture","title":"Organisation facultative","nodes":[{"id":"root","label":"CyCapture","x":0,"y":80},{"id":"images","label":"Images","x":260,"y":0},{"id":"videos","label":"Videos","x":260,"y":80},{"id":"gifs","label":"GIFs","x":260,"y":160},{"id":"audio","label":"Audio","x":260,"y":240},{"id":"day","label":"AAAA-MM-JJ","x":540,"y":80}],"edges":[{"from":"root","to":"images"},{"from":"root","to":"videos"},{"from":"root","to":"gifs"},{"from":"root","to":"audio"},{"from":"videos","to":"day"}]}
```

- **Séparer par type** crée Images, Videos, GIFs et Audio.
- **Dossiers quotidiens** ajoute un niveau `AAAA-MM-JJ`.
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
