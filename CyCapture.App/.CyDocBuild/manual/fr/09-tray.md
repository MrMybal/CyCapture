---
id: tray
title: Tray, état et notifications
description: Contrôlez CyCapture sans conserver de fenêtre ouverte.
section: Application
order: 90
sourceRevision: 2
---

L’icône de la zone de notification est la surface de contrôle principale de CyCapture.

## Menu du tray {#menu}

- Ouvrez la palette ou démarrez directement une Image, une Vidéo, un GIF ou un Audio.
- Ouvrez la Galerie, les Réglages, la documentation ou les dossiers de capture et de plugins.
- Activez les actions rapides des plugins comme CyAnnota Post Edit.
- Arrêtez un enregistrement actif.

## État d’enregistrement {#recording-state}

```diagram
{"type":"flow","title":"État du tray","nodes":[{"id":"ready","label":"Vert · Prêt","x":0,"y":50},{"id":"recording","label":"Rouge · REC + durée","x":300,"y":50},{"id":"saved","label":"Notification de fin","x":620,"y":50}],"edges":[{"from":"ready","to":"recording","label":"Démarrer"},{"from":"recording","to":"saved","label":"Arrêter"},{"from":"saved","to":"ready"}]}
```

La notification confirme le fichier produit. Cliquez dessus dans les six secondes pour ouvrir directement le fichier.

Fermer les Réglages ou la Galerie ne quitte pas CyCapture. Utilisez **Quitter CyCapture** dans le menu du tray pour arrêter le processus d’arrière-plan.
