# CyCapture

<p align="center">
  <img src="CyCapture.App/Assets/CyCaptureLogo.png" alt="Logo CyCapture" width="180">
</p>

CyCapture est un outil de capture discret écrit en **C# avec Avalonia 12** et ciblant Windows en priorité. CyAnnota reste un projet séparé ; son ouverture après capture passe uniquement par un plugin CyCapture facultatif.

## Utilisation

Au lancement, CyCapture reste dans la zone de notification et n’ouvre pas une grande interface.

1. Appuyez sur `Impr. écran`.
2. Choisissez la qualité vidéo, la qualité GIF et le style de sélection (**Intelligente**, **Fenêtre**, **Écran** ou **Zone libre**) dans la palette affichée près de la souris.
3. Choisissez **Image**, **Vidéo** ou **GIF**. Ce comportement peut être remplacé dans les réglages par un mode direct Image, Vidéo ou GIF.
4. Effectuez la sélection demandée.
5. Pour une vidéo ou un GIF, appuyez à nouveau sur `Impr. écran` pour arrêter.

Pendant l’enregistrement, l’icône du tray devient rouge, son infobulle affiche `REC` et la durée, et le menu du tray propose aussi l’arrêt. Le cadre rouge et son petit minuteur sont placés hors de la zone enregistrée et sont marqués comme exclus de la capture Windows.

Un clic droit sur l’icône du tray permet de lancer directement une capture d’image, une capture vidéo ou un GIF. Chaque commande ouvre la sélection intelligente dans le mode demandé.

Le même menu contient maintenant des entrées séparées pour la **Galerie** et les **Réglages**. La galerie affiche les miniatures des images et GIF présents dans le dossier de captures et permet de lire les vidéos avec le lecteur Windows par défaut. Après une capture, la notification reste cliquable pendant six secondes pour ouvrir directement le fichier.

## Fonctions de la version Avalonia

- application tray-first, sans fenêtre principale imposée ;
- remplacement natif de `Impr. écran` par un hook clavier Windows intégré au processus C# ;
- palette Image / Vidéo / GIF au niveau de la souris ;
- qualités vidéo/GIF et style de sélection directement modifiables dans la palette ;
- modes de sélection Intelligente, Fenêtre, Écran et Zone libre ;
- comportement de `Impr. écran` configurable : palette ou capture directe Image/Vidéo/GIF ;
- commandes Image/Vidéo/GIF directement accessibles par clic droit sur le tray ;
- galerie locale avec miniatures, actualisation et accès direct aux médias ;
- notifications de fin de capture cliquables ;
- entrée `Réglages…` dédiée dans le menu du tray ;
- sélection intelligente de la fenêtre, de sa zone cliente et de contrôles internes ;
- sélection libre et sélection d’écran ;
- prise en charge des bureaux multi-écrans décalés et du DPI par moniteur ;
- capture image PNG/JPEG et copie dans le presse-papiers ;
- vidéo MP4 H.264 via Media Foundation, avec son système et microphone optionnels ;
- GIF direct avec profils compact, équilibré et haute qualité ;
- tray normal vert et tray d’enregistrement rouge avec minuterie ;
- historique local et métadonnées configurables : intégrées au média par défaut, base centrale, JSON adjacent ou aucune métadonnée ;
- plugins C# de post-traitement via `ICapturePostProcessor` dans `%LOCALAPPDATA%\CyCapture\Plugins` ;
- activation et configuration individuelles des plugins depuis les réglages ;
- contribution facultative des plugins sous forme de case globale dans le tray et la palette rapide ;
- plugin **CyAnnota Post Edit** fourni, désactivé par défaut, pour ouvrir automatiquement les images et vidéos terminées ;
- petite fenêtre de réglages accessible depuis le tray ;
- instance unique : relancer CyCapture ouvre les réglages de l’instance existante.

## Compiler

Prérequis : SDK .NET 8 ou plus récent et Windows x64.

```powershell
git clone https://github.com/MrMybal/CyCapture.git
cd CyCapture
dotnet restore CyCapture.sln --runtime win-x64
dotnet build CyCapture.sln -c Release
```

## Produire la version portable

```powershell
dotnet publish CyCapture.App/CyCapture.App.csproj `
  -c Release -p:Platform=x64 -r win-x64 --self-contained true `
  -o dist-native/CyCapture-1.4.1-windows-x64
```

Le projet publie un exécutable autonome en fichier unique. ScreenRecorderLib utilise Media Foundation et demande le Media Feature Pack sur une édition Windows N/KN qui ne l’inclut pas.

## Architecture et portabilité

L’interface, les modèles et le pipeline de plugins utilisent Avalonia/C#. Les services `Platform/Windows` contiennent le hook clavier, l’énumération des fenêtres et la capture Windows. Cette séparation permettra d’ajouter plus tard des implémentations macOS et Linux sans refaire l’interface.

Ce dossier de travail contient uniquement la version native Avalonia/C#. Les anciens fichiers Electron sont restés dans le dossier de migration d’origine et ne font pas partie de ce projet.

## Métadonnées et intégration CyAnnota

Le plugin **Manifeste de capture** permet de choisir son stockage depuis les réglages :

- **Intégré au média** : écrit dans le PNG, JPEG, GIF ou MP4 sans ajouter de JSON dans le dossier des captures ;
- **Base centrale** : écrit dans `%LOCALAPPDATA%\CyCapture\Metadata` ;
- **JSON adjacent** : conserve le comportement historique `.cycapture.json` ;
- **Aucune métadonnée** : ne produit aucun manifeste.

Le plugin **CyAnnota Post Edit** est implémenté exclusivement dans CyCapture et ne modifie, ne copie et ne référence à la compilation aucun fichier source de CyAnnota. Il est désactivé par défaut. Une fois activé, sa case **PostEdit with CyAnnota** est synchronisée entre les réglages, le tray et la palette rapide. Le chemin de `CyAnnota.exe`, le mode de lancement et les types de médias à ouvrir sont configurables. Les GIF sont ignorés tant qu’ils ne sont pas pris en charge par CyAnnota.

Le contrat générique destiné aux autres plugins est décrit dans [`PLUGIN_API.md`](PLUGIN_API.md).

## Licence

Copyright © 2026 CyberAlien.

CyCapture est distribué sous la **GNU Affero General Public License v3.0 uniquement** (`AGPL-3.0-only`). Le texte complet est disponible dans [`LICENSE`](LICENSE).
