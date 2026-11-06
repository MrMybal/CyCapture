---
id: troubleshooting
title: Shortcuts and troubleshooting
description: Quick checks for capture, sound, permissions, codecs and displays.
section: Support
order: 140
revision: 2
---

## Keyboard reference {#shortcuts}

| Key | Action |
| --- | --- |
| Print Screen | Open/start capture; stop active Video, GIF or Audio recording. |
| Escape | Cancel selection or close the Gallery viewer. |
| Left / Right | Navigate media in Gallery. |
| F1 | Open contextual local documentation from supported windows. |

## If capture fails {#capture-fails}

1. Confirm the output folder is writable and another utility does not own Print Screen.
2. Try Screen selection and a short recording to isolate smart-target issues.
3. Update the graphics driver if Windows Graphics Capture repeatedly fails.
4. Restart CyCapture after changing display topology or audio devices.

## If sound or playback fails {#sound-fails}

1. Check the system and microphone sources for the selected mode.
2. Test system audio and microphone separately before mixing them.
3. Windows N/KN editions need the Media Feature Pack for Media Foundation.
4. Keep the original file when reporting a codec or timing problem.

## If local help does not open {#help-fails}

CyCapture opens `Documentation/Documentation.html` beside the application through a `file://` URL. Check that the complete `Documentation` folder is present and that the default browser accepts local files.
