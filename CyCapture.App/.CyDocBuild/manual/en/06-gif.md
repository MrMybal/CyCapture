---
id: gif
title: GIF capture
description: Create a ready-to-share animation directly from a selected region.
section: Capture
parent: selection
order: 60
revision: 2
---

GIF capture is silent and has its own dimension and frame-rate profiles.

## Quality profiles {#quality}

| Profile | Frame rate | Maximum long side |
| --- | ---: | ---: |
| Compact | 8 fps | 720 px |
| Balanced | 10 fps | 960 px |
| High | 12 fps | 1280 px |

## Recording {#recording}

Select Smart, Window, Screen or Free region as for a video. A dedicated red frame can be enabled or disabled in GIF settings. Press Print Screen again to stop and let CyCapture encode the animation.

```diagram
{"type":"flow","title":"GIF pipeline","nodes":[{"id":"select","label":"Select area","x":0,"y":50},{"id":"frames","label":"Capture frames","x":240,"y":50},{"id":"optimize","label":"Resize and optimize","x":490,"y":50},{"id":"gif","label":"GIF","x":750,"y":50}],"edges":[{"from":"select","to":"frames"},{"from":"frames","to":"optimize"},{"from":"optimize","to":"gif"}]}
```

```callout
{"type":"note","title":"No audio","body":"GIF capture never records system sound or microphone input."}
```
