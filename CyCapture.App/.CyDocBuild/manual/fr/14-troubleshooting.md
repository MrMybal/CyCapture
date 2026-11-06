---
id: troubleshooting
title: Raccourcis et dépannage
description: Contrôles rapides pour la capture, le son, les permissions, les codecs et les écrans.
section: Assistance
order: 140
sourceRevision: 2
---

## Raccourcis {#shortcuts}

| Touche | Action |
| --- | --- |
| Impr. écran | Ouvrir/démarrer la capture ; arrêter une Vidéo, un GIF ou un Audio actif. |
| Échap | Annuler la sélection ou fermer la visionneuse de la Galerie. |
| Gauche / Droite | Parcourir les médias dans la Galerie. |
| F1 | Ouvrir l’aide locale contextuelle depuis les fenêtres compatibles. |

## Si la capture échoue {#capture-fails}

1. Vérifiez que le dossier de sortie est accessible et qu’un autre outil ne monopolise pas Impr. écran.
2. Testez la sélection Écran et un enregistrement court pour isoler la cible intelligente.
3. Mettez à jour le pilote graphique si Windows Graphics Capture échoue régulièrement.
4. Redémarrez CyCapture après un changement d’écrans ou de périphériques audio.

## Si le son ou la lecture échoue {#sound-fails}

1. Vérifiez les sources système et microphone du mode concerné.
2. Testez le son système et le microphone séparément avant de les mixer.
3. Les éditions Windows N/KN exigent le Media Feature Pack pour Media Foundation.
4. Conservez le fichier original pour signaler un problème de codec ou de synchronisation.

## Si l’aide locale ne s’ouvre pas {#help-fails}

CyCapture ouvre `Documentation/Documentation.html` à côté de l’application avec une URL `file://`. Vérifiez que le dossier `Documentation` complet est présent et que le navigateur par défaut accepte les fichiers locaux.
