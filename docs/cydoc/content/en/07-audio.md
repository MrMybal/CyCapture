---
id: audio
title: Audio-only recording
description: Record system sound, microphone, or both without capturing the screen.
section: Capture
order: 70
revision: 1
---

## Sources {#sources}

At least one source must be enabled in the Audio tab.

- **System audio** records what Windows is playing.
- **Microphone** records the selected default input device.
- Both sources can be mixed into the same recording.

```diagram
{"type":"flow","title":"Audio-only pipeline","nodes":[{"id":"system","label":"System sound","x":0,"y":0},{"id":"mic","label":"Microphone","x":0,"y":130},{"id":"mix","label":"Mix","x":280,"y":65},{"id":"encode","label":"MP3 encoder","x":520,"y":65},{"id":"file","label":"MP3 or WAV","x":760,"y":65}],"edges":[{"from":"system","to":"mix"},{"from":"mic","to":"mix"},{"from":"mix","to":"encode"},{"from":"encode","to":"file"}]}
```

## Encoding and control {#encoding}

Profiles range from 96 kbit/s mono to 192 kbit/s stereo. CyCapture writes MP3 and falls back to WAV if MP3 finalization is unavailable.

Audio starts immediately because no screen selection is required. Press Print Screen again or use the tray Stop command to finish.

Audio-only sources and quality are independent from the audio settings used inside [Video](doc:video#audio).
