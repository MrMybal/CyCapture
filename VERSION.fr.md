# CyCapture 1.4.11

**Stade :** Bêta<br>
**Plateforme :** Windows x64<br>
**Version publiée :** [v1.4.11](https://github.com/MrMybal/CyCapture/releases/tag/v1.4.11)

CyCapture est en développement bêta actif. L’application peut être utilisée pour les captures quotidiennes, mais le comportement de capture et les paquets doivent encore être validés sur chaque configuration Windows cible.

## Principales modifications de cette version

- Migration de l’aide intégrée vers le dernier flux CyDoc épinglé.
- Ajout de la recherche hiérarchique en anglais, en français et globale, de schémas, de galeries multimédias et de références de code et d’API.
- Distribution du lecteur documentaire local complet avec l’installateur, avec une copie embarquée de secours pour l’utilisation portable.
- Connexion de F1, des actions d’aide contextuelle, du README et de GitHub Pages à `Documentation.html`.
- Automatisation de la génération CyDoc et du déploiement GitHub Pages.

## Limites connues

- Les binaires officiels ciblent actuellement Windows x64 uniquement.
- Les éditions Windows N et KN peuvent nécessiter le Media Feature Pack de Microsoft pour la lecture dans la galerie.
- D’autres utilitaires de capture peuvent entrer en conflit avec la gestion globale de la touche Impr écran.
- L’intégration CyAnnota est facultative, désactivée par défaut et ne post-traite pas les captures GIF.
- Si le dossier externe `Documentation` n’est pas disponible, les versions portables extraient l’aide embarquée dans le dossier CyCapture local de l’utilisateur.

Signalez les problèmes reproductibles dans le [suivi des problèmes](https://github.com/MrMybal/CyCapture/issues).

## Téléchargements

- [Installateur Windows x64](https://github.com/MrMybal/CyCapture/releases/download/v1.4.11/CyCapture-1.4.11-windows-x64-installer.exe)
- [Version portable avec CyAnnota](https://github.com/MrMybal/CyCapture/releases/download/v1.4.11/CyCapture-1.4.11-windows-x64-portable.exe)
- [Version portable sans CyAnnota](https://github.com/MrMybal/CyCapture/releases/download/v1.4.11/CyCapture-1.4.11-windows-x64-portable-without-CyAnnota.exe)
- [Paquet du plugin CyAnnota](https://github.com/MrMybal/CyCapture/releases/download/v1.4.11/CyCapture.Plugin.CyAnnota-0.4.4.zip)
