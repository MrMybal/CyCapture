# Composants embarqués

## NAudio 2.3.0

CyCapture utilise NAudio pour la capture audio WASAPI (son système et microphone) et l’encodage MP3 via Media Foundation sous Windows.

- Projet et code source : <https://github.com/naudio/NAudio/tree/2.3.0>
- Licence : MIT
- Copyright © Mark Heath et les contributeurs NAudio

Le paquet NuGet et ses bibliothèques nécessaires sont inclus dans les builds autonomes de CyCapture.

---

## LibVLCSharp 3.10.1 et LibVLC 3.0.23.1

CyCapture utilise les vues Avalonia officielles LibVLCSharp et le moteur LibVLC Windows pour lire directement les vidéos, GIF animés et fichiers audio dans la galerie.

- Projet et code source : <https://code.videolan.org/videolan/LibVLCSharp>
- Moteur LibVLC : <https://www.videolan.org/vlc/libvlc.html>
- Licences : GNU Lesser General Public License v2.1 ou ultérieure pour LibVLCSharp et les composants LibVLC concernés
- Copyright © VideoLAN et les contributeurs

Les bibliothèques natives et les plugins de codecs fournis par les paquets officiels VideoLAN sont inclus dans les deux éditions Windows de CyCapture.

---

## CyAnnota 0.4.4

Les builds Windows Release de CyCapture embarquent la distribution Windows **CyAnnota 0.4.4** afin que le post-traitement fonctionne sans installation séparée ni association de protocole Windows. La distribution unpacked est privilégiée : elle est extraite une seule fois en arrière-plan, puis CyCapture lance directement `CyAnnota.exe`.

- Projet et code source correspondant : <https://github.com/MrMybal/CyAnnota/tree/v0.4.4>
- Release utilisée : <https://github.com/MrMybal/CyAnnota/releases/tag/v0.4.4>
- Distribution de référence : `CyAnnota-0.4.4-portable.exe`
- SHA-256 de la distribution portable officielle : `B239CB3DC3E1A2B5266AF1DCA70A152AB6F4F5D45C49F54F1D6871CB350E35E2`
- Licence : GNU Affero General Public License v3.0 uniquement (`AGPL-3.0-only`)
- Copyright © 2026 CyberAlien

CyAnnota conserve dans sa distribution ses propres textes de licence et mentions relatives à Electron, Chromium, FFmpeg et aux autres composants tiers. CyCapture ne modifie aucun fichier source de CyAnnota : il archive la distribution Windows produite par le projet, l’embarque puis l’extrait localement.

---

# Bundled components

## NAudio 2.3.0

CyCapture uses NAudio for WASAPI audio capture (system audio and microphone) and Media Foundation MP3 encoding on Windows.

- Project and source code: <https://github.com/naudio/NAudio/tree/2.3.0>
- License: MIT
- Copyright © Mark Heath and NAudio contributors

The NuGet package and its required libraries are included in CyCapture self-contained builds.

---

## LibVLCSharp 3.10.1 and LibVLC 3.0.23.1

CyCapture uses the official LibVLCSharp Avalonia views and the Windows LibVLC engine to play videos, animated GIFs and audio files directly inside the gallery.

- Project and source code: <https://code.videolan.org/videolan/LibVLCSharp>
- LibVLC engine: <https://www.videolan.org/vlc/libvlc.html>
- Licenses: GNU Lesser General Public License v2.1 or later for LibVLCSharp and the applicable LibVLC components
- Copyright © VideoLAN and contributors

The native libraries and codec plugins supplied by the official VideoLAN packages are included in both Windows editions of CyCapture.

---

## CyAnnota 0.4.4

Windows Release builds of CyCapture bundle the **CyAnnota 0.4.4** Windows distribution so that post-processing works without a separate installation or a Windows protocol association. The unpacked distribution is preferred: it is extracted once in the background, after which CyCapture starts `CyAnnota.exe` directly.

- Project and corresponding source code: <https://github.com/MrMybal/CyAnnota/tree/v0.4.4>
- Source release: <https://github.com/MrMybal/CyAnnota/releases/tag/v0.4.4>
- Reference distribution: `CyAnnota-0.4.4-portable.exe`
- Official portable distribution SHA-256: `B239CB3DC3E1A2B5266AF1DCA70A152AB6F4F5D45C49F54F1D6871CB350E35E2`
- License: GNU Affero General Public License v3.0 only (`AGPL-3.0-only`)
- Copyright © 2026 CyberAlien

CyAnnota retains its own license texts and notices for Electron, Chromium, FFmpeg and other third-party components inside its distribution. CyCapture does not modify any CyAnnota source file: it archives the Windows distribution produced by the project, bundles it and extracts it locally.
