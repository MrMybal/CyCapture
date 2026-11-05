---
id: image-annotation
title: Images and quick annotations
description: Capture PNG or JPEG and annotate the frozen desktop before selecting.
section: Capture
order: 40
revision: 1
---

## Image formats {#formats}

PNG is lossless. JPEG offers compact, balanced and high-quality profiles. PNG ignores the JPEG quality setting. Images can be copied automatically to the clipboard.

## Annotation workflow {#annotation-workflow}

1. Enable quick annotation tools in Image settings.
2. Choose Pen, Rectangle, Arrow or Text before selecting the capture area.
3. Draw anywhere on the frozen multi-monitor desktop; change color, undo or clear as needed.
4. Return to **Select**, then click or drag the final crop. Only annotations inside the crop are saved.

```image
{"id":"annotation-comparison","src":"assets/annotation-before.svg","compare":"assets/annotation-after.svg","labels":["Before","Annotated"],"alt":"Fictional interface before and after a quick annotation","controls":"minimal"}
```

```diagram
{"type":"flow","title":"Annotate before selecting","nodes":[{"id":"freeze","label":"Freeze desktop","x":0,"y":50},{"id":"draw","label":"Draw or add text","x":230,"y":50},{"id":"select","label":"Return to Select","x":470,"y":50},{"id":"save","label":"Crop and save","x":700,"y":50}],"edges":[{"from":"freeze","to":"draw"},{"from":"draw","to":"select"},{"from":"select","to":"save"}]}
```
