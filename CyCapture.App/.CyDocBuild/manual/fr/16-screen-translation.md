---
id: screen-translation
title: Copier ou traduire du texte à l’écran
description: Reconnaissez le texte d’une zone, copiez-le ou affichez sa traduction hors ligne par-dessus l’image figée.
section: Modes de capture
order: 75
sourceRevision: 3
---

## Copier le texte d’une zone {#copy-text}

Choisissez **Texte** dans la palette rapide ou **Copier le texte d’une zone** dans le menu du tray. Sélectionnez une fenêtre visible, un écran, un élément intelligent ou une zone libre. CyCapture reconnaît localement les lignes et les place directement dans le presse-papiers, sans créer de fichier image.

La fenêtre de traitement apparaît au centre de la zone sélectionnée, sur le même écran. Elle se ferme automatiquement lorsque la reconnaissance est terminée.

## Traduire une zone {#translate-area}

Choisissez **Traduction** dans la palette rapide ou **Traduire une zone** dans le menu du tray. Sélectionnez une fenêtre visible, un écran, un élément intelligent ou une zone libre exactement comme pour une capture d’image.

CyCapture fige l’écran sélectionné, reconnaît le texte localement, le traduit localement puis dessine des cadres lisibles par-dessus les lignes originales. Le fond n’est jamais effacé ni modifié.

```diagram
{"type":"flow","title":"Traduction locale de l’écran","nodes":[{"id":"select","label":"Sélectionner","x":0,"y":50},{"id":"ocr","label":"OCR local","x":220,"y":50},{"id":"translate","label":"Traduction locale","x":450,"y":50},{"id":"overlay","label":"Superposition sur l’écran figé","x":710,"y":50}],"edges":[{"from":"select","to":"ocr"},{"from":"ocr","to":"translate"},{"from":"translate","to":"overlay"}]}
```

## Langues et modèles {#models}

Ouvrez **Réglages > Traduction** pour choisir la langue source de l’OCR et la langue cible de la traduction. La détection automatique prépare initialement l’OCR anglais, chinois simplifié et traditionnel, japonais et coréen. Le choix d’une langue source précise ne télécharge que ses données OCR. La langue source est commune aux modes Texte et Traduction.

Utilisez **Gérer et télécharger les modèles de langues** pour préparer à l’avance autant de langues sources que nécessaire. Les directions de traduction utilisent les modèles Mozilla Bergamot. Lorsqu’aucune direction directe n’existe, CyCapture peut utiliser l’anglais comme pivot local, par exemple chinois → anglais → français.

Les modèles sont téléchargés une seule fois dans les données locales de CyCapture puis réutilisés hors ligne. La connexion Internet sert uniquement au téléchargement des modèles.

## Lire et copier {#overlay}

- Appuyez sur **Échap** ou cliquez sur **Fermer** pour quitter la superposition traduite.
- Cliquez sur **Copier le texte** ou utilisez **Ctrl+C** pour copier toutes les lignes traduites.
- Le réglage **Copier automatiquement le texte traduit dans le presse-papiers** effectue cette copie dès la fin de la traduction. Il est coché par défaut.
- La traduction est une superposition temporaire : aucun fichier de capture n’est créé.

```callout
{"type":"tip","title":"OCR chinois","body":"Pour une reconnaissance plus fiable, choisissez explicitement Chinois simplifié ou Chinois traditionnel lorsque vous connaissez l’écriture utilisée."}
```
