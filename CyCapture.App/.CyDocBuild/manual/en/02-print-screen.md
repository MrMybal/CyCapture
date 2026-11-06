---
id: print-screen
title: Print Screen behavior
description: Choose the palette, a direct mode, or the quick-press and hold gesture.
section: Start
parent: getting-started
order: 20
revision: 2
---

## Available behaviors {#behaviors}

- **Show mode picker:** always display Image, Video, GIF and Audio near the pointer.
- **Direct Image, Video, GIF or Audio:** skip the palette and start that workflow immediately.
- **Hybrid:** a quick press opens [[Smart selection]] for an image; holding opens the mode palette.

```diagram
{"type":"flow","title":"Hybrid Print Screen gesture","nodes":[{"id":"key","label":"Print Screen","x":0,"y":70},{"id":"tap","label":"Quick press","x":240,"y":0},{"id":"hold","label":"Hold","x":240,"y":140},{"id":"image","label":"Smart image selection","x":500,"y":0},{"id":"palette","label":"Mode palette","x":500,"y":140}],"edges":[{"from":"key","to":"tap"},{"from":"key","to":"hold"},{"from":"tap","to":"image"},{"from":"hold","to":"palette"}]}
```

## Hold delay {#hold-delay}

The hybrid threshold can be set from **0.1 to 1.0 second** in Settings. A shorter delay opens the palette faster; a longer delay makes accidental holds less likely.

## Context rules {#context-rules}

- During Video, GIF or Audio recording, Print Screen stops the recording.
- If the selection overlay is open, Print Screen cancels it.
- The global key remains active while CyCapture is running in the tray.
- The same actions are available from the tray menu.

See [Settings](doc:settings#print-screen) for configuration.
