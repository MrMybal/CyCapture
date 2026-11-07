# CyCapture 1.5.0

**Stade :** Bêta<br>
**Plateforme :** Windows x64<br>
**Version publiée :** [v1.5.0](https://github.com/MrMybal/CyCapture/releases/tag/v1.5.0)

CyCapture est en développement bêta actif. L’application peut être utilisée pour les captures quotidiennes, mais le comportement de capture et les paquets doivent encore être validés sur chaque configuration Windows cible.

## Principales modifications de cette version

- Ajout de l’OCR et de la traduction à l’écran entièrement locaux, avec une superposition dessinée sur le bureau figé sans effacer le fond d’origine.
- Ajout des modèles OCR et Bergamot téléchargeables, de la détection automatique, du choix explicite du chinois simplifié ou traditionnel et du passage local par l’anglais lorsqu’il est nécessaire.
- Ajout d’un gestionnaire de modèles de langues et du mode Traduction dans la palette rapide, le tray, les réglages et le comportement direct d’Impr écran.
- Ajout d’options de presse-papiers indépendantes pour l’Image, la Vidéo, le GIF, l’Audio et le texte traduit, cochées par défaut avec migration unique des anciens réglages.
- Amélioration de la capture audio seule avec ajustement automatique du niveau système, correction de la synchronisation MP3 et séparation des réglages audio/vidéo.
- Amélioration du cadre d’enregistrement, de la sélection multi-écrans, de l’ordre visible des fenêtres, de la proposition de mise à jour au démarrage, de la langue de l’application et de l’organisation des réglages par mode.
- Extension de la documentation bilingue hors ligne pour la traduction, les modèles, le presse-papiers, l’audio et les flux de capture.

## Limites connues

- Les binaires officiels ciblent actuellement Windows x64 uniquement.
- Les éditions Windows N et KN peuvent nécessiter le Media Feature Pack de Microsoft pour la lecture dans la galerie.
- D’autres utilitaires de capture peuvent entrer en conflit avec la gestion globale de la touche Impr écran.
- L’intégration CyAnnota est facultative, désactivée par défaut et ne post-traite pas les captures GIF.
- L’OCR et la traduction nécessitent un premier téléchargement des modèles de langues sélectionnés ; la qualité de reconnaissance dépend toujours de la taille du texte, du contraste et de la langue source choisie.
- Si le dossier externe `Documentation` n’est pas disponible, les versions portables extraient l’aide embarquée dans le dossier CyCapture local de l’utilisateur.

Signalez les problèmes reproductibles dans le [suivi des problèmes](https://github.com/MrMybal/CyCapture/issues).

## Téléchargements

- [Installateur Windows x64](https://github.com/MrMybal/CyCapture/releases/download/v1.5.0/CyCapture-1.5.0-windows-x64-installer.exe)
- [Version portable avec CyAnnota](https://github.com/MrMybal/CyCapture/releases/download/v1.5.0/CyCapture-1.5.0-windows-x64-portable.exe)
- [Version portable sans CyAnnota](https://github.com/MrMybal/CyCapture/releases/download/v1.5.0/CyCapture-1.5.0-windows-x64-portable-without-CyAnnota.exe)
- [Paquet du plugin CyAnnota](https://github.com/MrMybal/CyCapture/releases/download/v1.5.0/CyCapture.Plugin.CyAnnota-0.4.4.zip)
