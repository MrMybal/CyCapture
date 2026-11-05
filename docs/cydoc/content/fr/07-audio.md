---
id: audio
title: Capture audio seule
description: Enregistrez le son système, le microphone ou les deux sans capturer l’écran.
section: Capture
order: 70
sourceRevision: 1
---

## Sources {#sources}

Au moins une source doit être activée dans l’onglet Audio.

- **Son système** enregistre ce que Windows est en train de lire.
- **Microphone** enregistre l’entrée par défaut sélectionnée.
- Les deux sources peuvent être mixées dans le même fichier.

```diagram
{"type":"flow","title":"Pipeline audio seul","nodes":[{"id":"system","label":"Son système","x":0,"y":0},{"id":"mic","label":"Microphone","x":0,"y":130},{"id":"mix","label":"Mixage","x":280,"y":65},{"id":"encode","label":"Encodeur MP3","x":520,"y":65},{"id":"file","label":"MP3 ou WAV","x":760,"y":65}],"edges":[{"from":"system","to":"mix"},{"from":"mic","to":"mix"},{"from":"mix","to":"encode"},{"from":"encode","to":"file"}]}
```

## Encodage et contrôle {#encoding}

Les profils vont de 96 kbit/s mono à 192 kbit/s stéréo. CyCapture écrit du MP3 et se replie sur WAV si la finalisation MP3 est indisponible.

L’audio démarre immédiatement car aucune sélection d’écran n’est nécessaire. Appuyez de nouveau sur Impr. écran ou utilisez Arrêter dans le tray.

Les sources et la qualité Audio seul sont indépendantes des réglages de son de la [Vidéo](doc:video#audio).
