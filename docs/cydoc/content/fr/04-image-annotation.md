---
id: image-annotation
title: Images et annotations rapides
description: Capturez en PNG ou JPEG et annotez le bureau figé avant la sélection.
section: Capture
order: 40
sourceRevision: 1
---

## Formats d’image {#formats}

Le PNG est sans perte. Le JPEG propose des profils compact, équilibré et haute qualité. Le PNG ignore la qualité JPEG. Les images peuvent être copiées automatiquement dans le presse-papiers.

## Parcours d’annotation {#annotation-workflow}

1. Activez les outils d’annotation rapide dans les réglages Image.
2. Choisissez Crayon, Cadre, Flèche ou Texte avant de sélectionner la capture.
3. Dessinez sur tout le bureau multi-écrans figé ; changez la couleur, annulez ou effacez si nécessaire.
4. Revenez sur **Sélection**, puis cliquez ou tracez le cadrage final. Seules les annotations dans le cadrage sont enregistrées.

```image
{"id":"annotation-comparison","src":"assets/annotation-before.svg","compare":"assets/annotation-after.svg","labels":["Avant","Annoté"],"alt":"Interface fictive avant et après une annotation rapide","controls":"minimal"}
```

```diagram
{"type":"flow","title":"Annoter avant de sélectionner","nodes":[{"id":"freeze","label":"Figer le bureau","x":0,"y":50},{"id":"draw","label":"Dessiner ou écrire","x":230,"y":50},{"id":"select","label":"Revenir à Sélection","x":470,"y":50},{"id":"save","label":"Cadrer et enregistrer","x":700,"y":50}],"edges":[{"from":"freeze","to":"draw"},{"from":"draw","to":"select"},{"from":"select","to":"save"}]}
```
