# Composants embarqués

## NAudio 2.3.0

CyCapture utilise NAudio pour la capture audio WASAPI (son système et microphone) et l’encodage MP3 via Media Foundation sous Windows.

- Projet et code source : <https://github.com/naudio/NAudio/tree/2.3.0>
- Licence : MIT
- Copyright © Mark Heath et les contributeurs NAudio

Le paquet NuGet et ses bibliothèques nécessaires sont inclus dans les builds autonomes de CyCapture.

---

## Tesseract .NET / Tesseract OCR 5.2.0

CyCapture utilise Tesseract pour reconnaître localement le texte des zones choisies dans le mode Traduction.

- Wrapper .NET et code source : <https://github.com/charlesw/tesseract>
- Moteur OCR et données de langues : <https://github.com/tesseract-ocr/tesseract>
- Licence : Apache License 2.0

Le moteur natif est distribué avec l’application. Les fichiers `tessdata_fast` des langues choisies sont téléchargés à la demande depuis le dépôt officiel Tesseract et conservés dans les données locales de CyCapture ; ils ne sont pas inclus dans le dépôt source.

---

## BergamotTranslatorSharp / Bergamot 0.7.0

CyCapture utilise Bergamot pour traduire localement le texte reconnu et l’afficher par-dessus l’écran figé.

- Wrapper .NET : <https://github.com/Freeesia/BergamotTranslatorSharp>
- Moteur Bergamot : <https://github.com/browsermt/bergamot-translator>
- Registre officiel des modèles Mozilla : <https://mozilla.github.io/translations/model-registry/>
- Licence du wrapper et du moteur : Mozilla Public License 2.0

La bibliothèque native est distribuée avec l’application. Les modèles de traduction sont téléchargés à la demande depuis le registre officiel Mozilla, vérifiés lorsque le registre fournit une empreinte, puis conservés dans les données locales de CyCapture.

---

## LibVLCSharp 3.10.1 et LibVLC 3.0.23.1

CyCapture utilise les vues Avalonia officielles LibVLCSharp et le moteur LibVLC Windows pour lire directement les vidéos, GIF animés et fichiers audio dans la galerie.

- Projet et code source : <https://code.videolan.org/videolan/LibVLCSharp>
- Moteur LibVLC : <https://www.videolan.org/vlc/libvlc.html>
- Licences : GNU Lesser General Public License v2.1 ou ultérieure pour LibVLCSharp et les composants LibVLC concernés
- Copyright © VideoLAN et les contributeurs

Les bibliothèques natives et les plugins de codecs fournis par les paquets officiels VideoLAN sont inclus dans les deux éditions Windows de CyCapture.

---

## CyDoc 0.1.0

CyCapture utilise CyDoc pour générer et afficher sa documentation locale bilingue. Le lecteur, les pages compilées, les schémas et les ressources nécessaires sont distribués dans le dossier `Documentation` puis ouverts directement avec une URL `file://`.

- Projet et code source : <https://github.com/MrMybal/CyDoc>
- Version : 0.1.0
- Licence : MIT
- Copyright © 2026 CyDoc contributors

Les modules distribués sont limités à `image` et `gallery`. Les dépendances Node.js servent uniquement à la génération et ne sont pas requises pour lire l’aide.

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

## Tesseract .NET / Tesseract OCR 5.2.0

CyCapture uses Tesseract to recognize text locally inside areas selected in Translation mode.

- .NET wrapper and source code: <https://github.com/charlesw/tesseract>
- OCR engine and language data: <https://github.com/tesseract-ocr/tesseract>
- License: Apache License 2.0

The native engine is distributed with the application. Language files from the official `tessdata_fast` repository are downloaded on demand and retained in CyCapture local data; they are not included in the source repository.

---

## BergamotTranslatorSharp / Bergamot 0.7.0

CyCapture uses Bergamot to translate recognized text locally and display it over the frozen screen.

- .NET wrapper: <https://github.com/Freeesia/BergamotTranslatorSharp>
- Bergamot engine: <https://github.com/browsermt/bergamot-translator>
- Official Mozilla model registry: <https://mozilla.github.io/translations/model-registry/>
- Wrapper and engine license: Mozilla Public License 2.0

The native library is distributed with the application. Translation models are downloaded on demand from Mozilla's official registry, verified when the registry provides a digest, and retained in CyCapture local data.

---

## LibVLCSharp 3.10.1 and LibVLC 3.0.23.1

CyCapture uses the official LibVLCSharp Avalonia views and the Windows LibVLC engine to play videos, animated GIFs and audio files directly inside the gallery.

- Project and source code: <https://code.videolan.org/videolan/LibVLCSharp>
- LibVLC engine: <https://www.videolan.org/vlc/libvlc.html>
- Licenses: GNU Lesser General Public License v2.1 or later for LibVLCSharp and the applicable LibVLC components
- Copyright © VideoLAN and contributors

The native libraries and codec plugins supplied by the official VideoLAN packages are included in both Windows editions of CyCapture.

---

## CyDoc 0.1.0

CyCapture uses CyDoc to generate and display its local bilingual documentation. The reader, compiled pages, diagrams and required resources are distributed in the `Documentation` directory and opened directly through a `file://` URL.

- Project and source code: <https://github.com/MrMybal/CyDoc>
- Version: 0.1.0
- License: MIT
- Copyright © 2026 CyDoc contributors

Only the `image` and `gallery` modules are distributed. Node.js dependencies are used at generation time only and are not required to read the help.

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
