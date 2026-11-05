---
id: licenses
title: Licences et composants embarqués
description: Licence de CyCapture et principaux composants distribués avec les éditions Windows.
section: À propos
order: 170
sourceRevision: 1
---

## CyCapture {#cycapture}

CyCapture est distribué sous la **GNU Affero General Public License v3.0 uniquement** (`AGPL-3.0-only`). Copyright © 2026 CyberAlien.

## Composants embarqués {#components}

| Composant | Utilisation | Licence |
| --- | --- | --- |
| NAudio 2.3.0 | Capture WASAPI et encodage MP3 Media Foundation | MIT |
| LibVLCSharp 3.10.1 / LibVLC 3.0.23.1 | Lecture Vidéo, GIF animé et Audio dans la Galerie | LGPL-2.1-or-later pour les composants concernés |
| CyDoc 0.1.0 | Lecteur local de documentation bilingue | MIT |
| CyAnnota 0.4.4 | Post-édition facultative dans les éditions Windows complètes | AGPL-3.0-only |

## Distribution de CyDoc {#cydoc}

CyCapture distribue uniquement les modules CyDoc `image` et `gallery`. Le lecteur généré, les pages compilées et les ressources sont intégrés à l’exécutable. Node.js, le compilateur et les dépendances Markdown de build ne sont pas nécessaires pour lire le manuel.

La licence MIT de CyDoc se trouve dans les ressources d’aide sous `assets/licenses/CyDoc-MIT.txt`. Les mentions complètes restent également distribuées à côté des builds CyCapture dans `BUNDLED_COMPONENTS.md`.
