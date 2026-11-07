---
id: video
title: Video recording
description: Record a selected area or screen to MP4/H.264 with optional audio.
section: Capture
parent: selection
order: 50
revision: 3
---

## Start and stop {#recording}

Choose Video, select Smart, Window, Screen or Free region, then confirm. Press **Print Screen** again or use **Stop recording** in the tray to finalize the MP4.

```diagram
{"type":"flow","title":"Video pipeline","nodes":[{"id":"select","label":"Select area","x":0,"y":50},{"id":"capture","label":"Video + optional audio","x":240,"y":50},{"id":"encode","label":"H.264","x":520,"y":50},{"id":"file","label":"MP4","x":740,"y":50}],"edges":[{"from":"select","to":"capture"},{"from":"capture","to":"encode"},{"from":"encode","to":"file"}]}
```

## Quality and performance {#quality}

- Frame-rate profiles: **24, 30 or 60 fps**.
- H.264 bitrate profiles: **1, 2.5, 6 or 12 Mbit/s**.
- Frame rate and bitrate are independent settings.
- Windows Graphics Capture provides the acquisition path on supported Windows systems.

## Video audio {#audio}

System audio and microphone are independent Video options. If neither is enabled, the MP4 is silent. Audio profiles range from 96 kbit/s mono to 192 kbit/s stereo.

## Recording frame and clipboard {#frame}

The optional thin red frame and timer indicate the active region and are excluded from capture. The Video frame is configured separately from the GIF frame. Finished video files are copied to the Windows clipboard as file attachments by default; this can be disabled in the Video tab.

Wait for the finalization notification before opening a long recording.
