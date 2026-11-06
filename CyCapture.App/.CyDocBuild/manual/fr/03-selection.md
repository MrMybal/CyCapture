---
id: selection
title: Sélectionner la capture
description: Cibles intelligentes, fenêtres, écrans et zones libres sur plusieurs moniteurs.
section: Capture
parent: getting-started
order: 30
sourceRevision: 2
---

Déplacez la souris pour prévisualiser la cible active, puis cliquez une fois pour confirmer.

```image
{"id":"selection-preview","src":"assets/selection-overlay.svg","alt":"Sélection intelligente sur un bureau fictif à deux écrans","controls":"minimal"}
```

## Quatre styles de sélection {#styles}

- **Intelligente** détecte la fenêtre visible, sa zone cliente et les contrôles compatibles sous la souris.
- **Fenêtre** cible la fenêtre supérieure visible en respectant le Z-order et les occlusions Windows.
- **Écran** cible un moniteur complet.
- **Zone libre** permet de tracer n’importe quel rectangle.

```diagram
{"type":"flow","title":"Décision de sélection","nodes":[{"id":"start","label":"Choisir un style","x":0,"y":80},{"id":"smart","label":"Cible intelligente","x":240,"y":0},{"id":"window","label":"Fenêtre","x":240,"y":80},{"id":"screen","label":"Écran","x":240,"y":160},{"id":"free","label":"Zone libre","x":240,"y":240},{"id":"confirm","label":"Prévisualiser et confirmer","x":520,"y":100}],"edges":[{"from":"start","to":"smart"},{"from":"start","to":"window"},{"from":"start","to":"screen"},{"from":"start","to":"free"},{"from":"smart","to":"confirm"},{"from":"window","to":"confirm"},{"from":"screen","to":"confirm"},{"from":"free","to":"confirm"}]}
```

## Comportement multi-écrans {#multi-monitor}

CyCapture travaille dans l’espace du bureau virtuel Windows. La sélection fonctionne sur chaque moniteur, y compris à gauche ou au-dessus de l’écran principal. Le DPI par moniteur et les bureaux décalés sont pris en compte.

Appuyez sur **Échap** ou **Impr. écran** pour annuler sans créer de fichier.
