# Composants embarqués

## CyAnnota 0.3.6

Les builds Windows Release de CyCapture embarquent la distribution Windows **CyAnnota 0.3.6** afin que le post-traitement fonctionne sans installation séparée ni association de protocole Windows. La distribution unpacked est privilégiée : elle est extraite une seule fois en arrière-plan, puis CyCapture lance directement `CyAnnota.exe`.

- Projet et code source correspondant : <https://github.com/MrMybal/CyAnnota/tree/v0.3.6>
- Release utilisée : <https://github.com/MrMybal/CyAnnota/releases/tag/v0.3.6>
- Distribution de référence : `CyAnnota-0.3.6-portable.exe`
- SHA-256 de la distribution portable officielle : `60A07568B71EB98F2F86EBA8869F61392548BDA7EC3FFD78274245351E4A93F2`
- Licence : GNU Affero General Public License v3.0 uniquement (`AGPL-3.0-only`)
- Copyright © 2026 CyberAlien

CyAnnota conserve dans sa distribution ses propres textes de licence et mentions relatives à Electron, Chromium, FFmpeg et aux autres composants tiers. CyCapture ne modifie aucun fichier source de CyAnnota : il archive la distribution Windows produite par le projet, l’embarque puis l’extrait localement.

---

# Bundled components

## CyAnnota 0.3.6

Windows Release builds of CyCapture bundle the **CyAnnota 0.3.6** Windows distribution so that post-processing works without a separate installation or a Windows protocol association. The unpacked distribution is preferred: it is extracted once in the background, after which CyCapture starts `CyAnnota.exe` directly.

- Project and corresponding source code: <https://github.com/MrMybal/CyAnnota/tree/v0.3.6>
- Source release: <https://github.com/MrMybal/CyAnnota/releases/tag/v0.3.6>
- Reference distribution: `CyAnnota-0.3.6-portable.exe`
- Official portable distribution SHA-256: `60A07568B71EB98F2F86EBA8869F61392548BDA7EC3FFD78274245351E4A93F2`
- License: GNU Affero General Public License v3.0 only (`AGPL-3.0-only`)
- Copyright © 2026 CyberAlien

CyAnnota retains its own license texts and notices for Electron, Chromium, FFmpeg and other third-party components inside its distribution. CyCapture does not modify any CyAnnota source file: it archives the Windows distribution produced by the project, bundles it and extracts it locally.
