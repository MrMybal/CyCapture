---
id: settings
title: Settings and Windows startup
description: Configure each media mode separately and keep CyCapture unobtrusive.
section: Application
parent: tray
order: 120
revision: 3
---

```image
{"id":"settings-overview","src":"assets/settings-modes.svg","alt":"Fictional CyCapture per-mode settings","controls":"minimal"}
```

## Settings by mode {#per-mode}

Image, Video, GIF, Audio and Translation have separate tabs and independent profiles. Video audio does not modify Audio-only recording, and Video/GIF recording frames are independent. Translation settings choose source/target languages and open the local model manager. The quick palette exposes the most useful controls before capture.

Each mode also has its own clipboard option. Image pixels, finished Video/GIF/Audio files, and translated text are copied automatically by default; disable only the modes you do not want copied.

## Print Screen {#print-screen}

Choose the palette, a direct mode, or the quick-press/hold behavior. The hold delay ranges from 0.1 to 1.0 second. See the [complete behavior reference](doc:print-screen#behaviors).

## Common settings {#common-settings}

- Choose the default visual selection style and capture folder.
- Organize files by type and/or by day.
- Configure history and metadata storage.
- Enable, disable and configure plugins.
- Choose whether CyCapture starts with Windows.

## Windows startup and instance {#startup}

Start with Windows launches CyCapture directly into the current user's notification area. Only one instance runs; launching CyCapture again opens the existing instance. Preferences are local to the current Windows user.
