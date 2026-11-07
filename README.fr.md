<table class="readme-header" width="100%">
  <tr>
    <td width="300" align="left" valign="top">
      <a href="https://mrmybal.github.io/CyCapture/Documentation.html#/fr/getting-started">Documentation</a><br>
      <a href="https://github.com/MrMybal/CyCapture">Dépôt</a><br>
      <a href="https://github.com/MrMybal/CyCapture/issues">Problèmes</a><br><br>
      <sub>README</sub><br>
      <a href="README.md">English</a> · <a href="README.fr.md">Français</a>
    </td>
    <td width="400" align="center" valign="middle">
      <a href="https://mrmybal.github.io/CyCapture/Documentation.html#/fr/getting-started">
        <img src="CyCapture.App/Assets/CyCaptureLogo.png" alt="CyCapture" width="128">
      </a>
    </td>
    <td width="300" align="right" valign="top">
      <p><sub>Licence de l’application</sub><br><a href="LICENSE"><strong>GNU AGPL-3.0-only</strong></a></p>
      <p><sub>Version</sub><br><a href="VERSION.fr.md"><strong>1.5.1</strong></a><br><strong>BÊTA</strong></p>
      <p><a href="https://github.com/MrMybal/CyCapture/releases/download/v1.5.1/CyCapture-1.5.1-windows-x64-installer.exe"><strong>Télécharger</strong></a><br><sub>Windows x64 · installateur</sub></p>
    </td>
  </tr>
</table>

<h1 align="center">CyCapture</h1>
<p align="center">Par <a href="https://github.com/MrMybal">CyberAlien</a></p>

CyCapture est une application de capture Windows discrète, pensée pour fonctionner depuis la zone de notification et remplacer le flux de travail habituel de la touche Impr écran par la capture d’image, de vidéo, de GIF et d’audio.

La version **1.5.1** est actuellement en **bêta**. Consultez les [notes de version](VERSION.fr.md) pour connaître le périmètre actuel et les limites connues.

## À propos de CyCapture

Lancez CyCapture une fois : l’application reste ensuite disponible dans la zone de notification Windows. Appuyez sur Impr écran pour ouvrir la palette de capture, démarrer directement une action ou utiliser le comportement hybride configurable : un appui court lance la sélection intelligente d’image et un appui prolongé ouvre la palette des modes.

Le sélecteur visuel peut cibler une fenêtre visible, un écran ou une zone libre sur plusieurs moniteurs. Un nouvel appui sur Impr écran arrête une capture vidéo, GIF ou audio en cours. Les images peuvent également être annotées directement sur le bureau figé avant de choisir la zone finale.

Les médias capturés sont disponibles dans la galerie intégrée et peuvent être copiés dans le presse-papiers. Le système de plugins prend en charge les traitements après capture. Le paquet Windows complet peut inclure l’intégration facultative de [CyAnnota](https://github.com/MrMybal/CyAnnota) ; CyAnnota reste un projet séparé et son plugin est désactivé par défaut.

Le guide illustré complet est disponible dans la [documentation française](https://mrmybal.github.io/CyCapture/Documentation.html#/fr/getting-started).

## Fonctionnalités

- Modes de capture d’image, de vidéo, de GIF animé, d’audio seul et de traduction à l’écran.
- OCR et traduction entièrement locaux avec superposition et modèles de langues téléchargeables, dont le chinois simplifié et traditionnel.
- Sélection intelligente des fenêtres visibles tenant compte de leur ordre et de leur occlusion.
- Capture d’écran entier et de zone libre sur les bureaux à plusieurs moniteurs.
- Annotations rapides sur le bureau figé : dessin libre, cadres, flèches et texte.
- Réglages indépendants pour l’image, la vidéo, le GIF, l’audio, la fréquence d’images, le débit et la qualité.
- Fonctionnement dans la zone de notification, gestion globale d’Impr écran, raccourcis de modes directs et état d’enregistrement.
- Galerie multimédia avec aperçu des images, GIF, vidéos et sons, filtres par date et actions de presse-papiers.
- Dossiers configurables, classement par type et par jour, métadonnées intégrées ou séparées et intégration au presse-papiers.
- Découverte et configuration de plugins C#, dont le post-traitement facultatif avec CyAnnota.
- Documentation CyDoc bilingue, consultable, entièrement locale, avec schémas et aide contextuelle.

---

## Installation

CyCapture cible actuellement **Windows x64**. Les éditions Windows N et KN peuvent nécessiter le Media Feature Pack de Microsoft pour la lecture vidéo.

### Recommandé : installateur

Téléchargez puis exécutez [CyCapture 1.5.1 pour Windows x64](https://github.com/MrMybal/CyCapture/releases/download/v1.5.1/CyCapture-1.5.1-windows-x64-installer.exe). L’installateur enregistre l’application, les raccourcis du menu Démarrer, la désinstallation et les composants facultatifs sélectionnés.

### Éditions portables

- [Édition portable avec CyAnnota](https://github.com/MrMybal/CyCapture/releases/download/v1.5.1/CyCapture-1.5.1-windows-x64-portable.exe)
- [Édition portable sans CyAnnota](https://github.com/MrMybal/CyCapture/releases/download/v1.5.1/CyCapture-1.5.1-windows-x64-portable-without-CyAnnota.exe)
- [Paquet du plugin CyAnnota](https://github.com/MrMybal/CyCapture/releases/download/v1.5.1/CyCapture.Plugin.CyAnnota-0.4.4.zip)

Extrayez une archive portable dans un dossier accessible en écriture, puis lancez `CyCapture.exe`. Les binaires de l’application sont autonomes. L’aide hors ligne est distribuée avec l’application et peut également utiliser sa copie embarquée de secours.

<details>
<summary><strong>Compiler depuis les sources</strong></summary>

### Prérequis

- SDK .NET 8 ou plus récent.
- Node.js 22 et npm pour l’export de la documentation CyDoc.
- Git.
- Inno Setup 6 uniquement pour construire l’installateur Windows.
- Un exécutable portable CyAnnota uniquement lors de la création explicite du paquet complet.

CyDoc est une dépendance de compilation publique et épinglée. Son lecteur exporté et ses ressources sont copiés dans CyCapture afin que la documentation installée reste disponible hors ligne par une URL locale `file://`.

### Commandes

```powershell
git clone https://github.com/MrMybal/CyCapture.git
cd CyCapture
dotnet restore CyCapture.sln --runtime win-x64
dotnet build CyCapture.sln -c Release
dotnet run --project tests/CyCapture.SettingsChecks/CyCapture.SettingsChecks.csproj -c Release -- .
./scripts/build-documentation.ps1
```

Pour produire le paquet Windows complet, indiquez explicitement le chemin de CyAnnota :

```powershell
./scripts/publish-windows-local.ps1 -CyAnnotaSource "C:\chemin\vers\CyAnnota-0.4.4-portable.exe"
```

</details>

---

## Licence et composants tiers

CyCapture est distribué sous la [GNU Affero General Public License v3.0 uniquement](LICENSE). Copyright © CyberAlien.

| Composant | Version | Licence | Utilisation dans CyCapture |
|---|---:|---|---|
| Avalonia | 12.1.1 | MIT | Cadriciel d’interface multiplateforme de l’application |
| Environnement .NET | 8 | MIT et notices des composants | Environnement autonome de l’application |
| ScreenRecorderLib | 6.6.0 | MIT | Enregistrement de l’écran et de la vidéo sous Windows |
| NAudio | 2.3.0 | MIT | Capture et traitement audio sous Windows |
| LibVLCSharp / LibVLC | 3.10.1 / 3.0.23.1 | Composants LGPL-2.1-or-later | Lecture vidéo et audio dans la galerie |
| System.Drawing.Common | 8.0.20 | MIT | Interopérabilité des images sous Windows |
| Tesseract .NET / Tesseract OCR | 5.2.0 | Apache-2.0 | Reconnaissance locale du texte avec données de langues téléchargeables |
| BergamotTranslatorSharp / Bergamot | 0.7.0 | MPL-2.0 | Traduction neuronale locale avec modèles Mozilla téléchargeables |
| CyDoc | 0.1.0 | MIT | Export lors de la compilation et lecteur local ; modules image et galerie uniquement |
| CyAnnota | 0.4.4 | AGPL-3.0-only | Post-édition facultative dans les paquets complet et plugin |

Consultez [BUNDLED_COMPONENTS.md](BUNDLED_COMPONENTS.md) pour les détails de distribution et les références des sources.

## Références

- [Manuel français](https://mrmybal.github.io/CyCapture/Documentation.html#/fr/getting-started)
- [English manual](https://mrmybal.github.io/CyCapture/Documentation.html#/en/getting-started)
- [Versions et téléchargements](https://github.com/MrMybal/CyCapture/releases)
- [Suivi des problèmes](https://github.com/MrMybal/CyCapture/issues)
- [API des plugins](PLUGIN_API.md)
- [Composants distribués](BUNDLED_COMPONENTS.md)
- [CyAnnota](https://github.com/MrMybal/CyAnnota)
- [CyDoc](https://github.com/MrMybal/CyDoc)
