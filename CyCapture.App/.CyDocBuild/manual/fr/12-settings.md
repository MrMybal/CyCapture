---
id: settings
title: Réglages et démarrage Windows
description: Configurez chaque média séparément tout en gardant CyCapture discret.
section: Application
parent: tray
order: 120
sourceRevision: 3
---

```image
{"id":"settings-overview","src":"assets/settings-modes.svg","alt":"Réglages CyCapture fictifs par mode","controls":"minimal"}
```

## Réglages par mode {#per-mode}

Image, Vidéo, GIF et Audio possèdent des onglets et profils indépendants. Le son Vidéo ne modifie pas l’Audio seul, et les cadres Vidéo/GIF sont séparés. Texte et Traduction partagent la langue source de l’OCR ; Traduction utilise aussi la langue cible. Le gestionnaire de modèles locaux prépare les deux parcours. La palette rapide expose les contrôles les plus utiles avant la capture.

Chaque mode possède également sa propre option de presse-papiers. Les pixels des images, les fichiers Vidéo/GIF/Audio terminés et le texte traduit sont copiés automatiquement par défaut ; décochez uniquement les modes que vous ne souhaitez pas copier.

## Impr. écran {#print-screen}

Choisissez la palette, un mode direct ou le geste appui court/maintien. Le délai du maintien va de 0,1 à 1,0 seconde. Consultez la [référence complète](doc:print-screen#behaviors).

## Réglages communs {#common-settings}

- Choisissez la sélection visuelle par défaut et le dossier des captures.
- Organisez les fichiers par type et/ou par jour.
- Configurez l’historique et les métadonnées.
- Activez, désactivez et configurez les plugins.
- Choisissez si CyCapture démarre avec Windows.

## Démarrage Windows et instance {#startup}

Lancer avec Windows démarre CyCapture directement dans le tray de l’utilisateur courant. Une seule instance fonctionne ; relancer CyCapture ouvre l’instance existante. Les préférences sont locales à l’utilisateur Windows courant.
