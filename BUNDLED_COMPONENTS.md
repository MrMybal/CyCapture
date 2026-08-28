# Composants embarqués

## NAudio 2.3.0

CyCapture utilise NAudio pour la capture audio WASAPI (son système et microphone) et l’encodage MP3 via Media Foundation sous Windows.

- Projet et code source : <https://github.com/naudio/NAudio/tree/2.3.0>
- Licence : MIT
- Copyright © Mark Heath et les contributeurs NAudio

Le paquet NuGet et ses bibliothèques nécessaires sont inclus dans les builds autonomes de CyCapture.

---

## CyAnnota 0.3.7

Les builds Windows Release de CyCapture embarquent la distribution Windows **CyAnnota 0.3.7** afin que le post-traitement fonctionne sans installation séparée ni association de protocole Windows. La distribution unpacked est privilégiée : elle est extraite une seule fois en arrière-plan, puis CyCapture lance directement `CyAnnota.exe`.

- Projet et code source correspondant : <https://github.com/MrMybal/CyAnnota/tree/v0.3.7>
- Release utilisée : <https://github.com/MrMybal/CyAnnota/releases/tag/v0.3.7>
- Distribution de référence : `CyAnnota-0.3.7-portable.exe`
- SHA-256 de la distribution portable officielle : `A07452E00BB463B63D6ED68092A3119033471A7B04A808D00ECD9B493FC93C02`
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

## CyAnnota 0.3.7

Windows Release builds of CyCapture bundle the **CyAnnota 0.3.7** Windows distribution so that post-processing works without a separate installation or a Windows protocol association. The unpacked distribution is preferred: it is extracted once in the background, after which CyCapture starts `CyAnnota.exe` directly.

- Project and corresponding source code: <https://github.com/MrMybal/CyAnnota/tree/v0.3.7>
- Source release: <https://github.com/MrMybal/CyAnnota/releases/tag/v0.3.7>
- Reference distribution: `CyAnnota-0.3.7-portable.exe`
- Official portable distribution SHA-256: `A07452E00BB463B63D6ED68092A3119033471A7B04A808D00ECD9B493FC93C02`
- License: GNU Affero General Public License v3.0 only (`AGPL-3.0-only`)
- Copyright © 2026 CyberAlien

CyAnnota retains its own license texts and notices for Electron, Chromium, FFmpeg and other third-party components inside its distribution. CyCapture does not modify any CyAnnota source file: it archives the Windows distribution produced by the project, bundles it and extracts it locally.
