---
id: licenses
title: Licensing and bundled components
description: CyCapture license and the principal components shipped with Windows builds.
section: About
order: 170
revision: 2
---

## CyCapture {#cycapture}

CyCapture is distributed under the **GNU Affero General Public License v3.0 only** (`AGPL-3.0-only`). Copyright © 2026 CyberAlien.

## Bundled components {#components}

| Component | Purpose | License |
| --- | --- | --- |
| NAudio 2.3.0 | WASAPI audio capture and Media Foundation MP3 encoding | MIT |
| LibVLCSharp 3.10.1 / LibVLC 3.0.23.1 | Gallery playback for video, animated GIF and audio | LGPL-2.1-or-later for the applicable components |
| Tesseract .NET / Tesseract OCR 5.2.0 | Local OCR for text copy and on-screen translation | Apache-2.0 |
| BergamotTranslatorSharp / Bergamot 0.7.0 | Local neural translation | MPL-2.0 |
| CyDoc 0.1.0 | Local bilingual documentation reader | MIT |
| CyAnnota 0.4.4 | Optional post-editing application in full Windows editions | AGPL-3.0-only |

## CyDoc distribution {#cydoc}

CyCapture ships only the `image` and `gallery` CyDoc modules. The generated reader, compiled pages and assets are installed in the `Documentation` folder. Node.js, the compiler and build-time Markdown dependencies are not needed to read the manual.

The CyDoc MIT license is distributed at `Documentation/DocData/assets/licenses/CyDoc-MIT.txt`. Complete component notices also remain beside installed or published CyCapture builds in `BUNDLED_COMPONENTS.md`.

OCR language data and Mozilla translation models are downloaded only when requested and remain in the local CyCapture data directory. They are not part of the source repository.
