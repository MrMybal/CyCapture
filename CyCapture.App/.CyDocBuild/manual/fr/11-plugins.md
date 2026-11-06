---
id: plugins
title: Plugins et CyAnnota
description: Activez les post-traitements et configurez chaque extension indépendamment.
section: Extensibilité
order: 110
sourceRevision: 2
---

Les plugins s’exécutent après l’enregistrement du fichier. Ils ne modifient pas le mode de capture sélectionné.

```diagram
{"type":"flow","title":"Pipeline de post-traitement","nodes":[{"id":"capture","label":"Capture enregistrée","x":0,"y":50},{"id":"pipeline","label":"Plugins actifs","x":260,"y":50},{"id":"cyannota","label":"CyAnnota facultatif","x":520,"y":50},{"id":"result","label":"Résultat","x":760,"y":50}],"edges":[{"from":"capture","to":"pipeline"},{"from":"pipeline","to":"cyannota"},{"from":"cyannota","to":"result"}]}
```

## Gestion des plugins {#management}

Les Réglages listent chaque `ICapturePostProcessor` détecté. Activez ou désactivez les plugins séparément et ouvrez leur configuration uniquement lorsqu’ils exposent des réglages. Les DLL autonomes sont chargées depuis le dossier Plugins de CyCapture.

## CyAnnota Post Edit {#cyannota}

L’édition complète inclut l’intégration CyAnnota facultative. Activez le plugin, puis **PostEdit with CyAnnota** dans les Réglages, le tray ou la palette. Au premier usage, l’application intégrée est préparée, démarrée et reçoit l’image ou la vidéo terminée.

Un exécutable CyAnnota personnalisé peut être choisi. Les GIF sont ignorés jusqu’à leur prise en charge.

## Ordre et erreurs {#failures}

Les processeurs actifs s’exécutent séquentiellement. Une erreur de plugin est signalée sans supprimer la capture originale. Installez uniquement des extensions de confiance.

Les développeurs peuvent consulter l’[API de plugins](doc:plugin-api#contract).
