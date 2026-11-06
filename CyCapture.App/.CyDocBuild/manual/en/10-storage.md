---
id: storage
title: Folders, history and metadata
description: Organize captures and choose how technical metadata is stored.
section: Library
parent: gallery
order: 100
revision: 2
---

## Folder organization {#folders}

Choose any writable absolute capture folder in Settings.

```diagram
{"type":"architecture","title":"Optional folder layout","nodes":[{"id":"root","label":"CyCapture","x":0,"y":100},{"id":"day","label":"YYYY-MM-DD","x":240,"y":100},{"id":"images","label":"Images","x":500,"y":0},{"id":"videos","label":"Videos","x":500,"y":70},{"id":"gifs","label":"GIFs","x":500,"y":140},{"id":"audio","label":"Audio","x":500,"y":210}],"edges":[{"from":"root","to":"day"},{"from":"day","to":"images"},{"from":"day","to":"videos"},{"from":"day","to":"gifs"},{"from":"day","to":"audio"}]}
```

- **Daily folders** create a `YYYY-MM-DD` folder below the capture root.
- **Separate by type** creates Images, Videos, GIFs and Audio below the active day folder, or directly below the root when daily folders are disabled.
- Both options can be combined.

## Capture metadata {#metadata}

The Capture Manifest plugin offers four modes:

| Mode | Result |
| --- | --- |
| Embedded in media | Stores metadata inside PNG, JPEG, GIF or MP4 when supported. |
| Central database | Stores side information under the CyCapture application-data folder. |
| Adjacent JSON | Writes a `.cycapture.json` file beside the capture. |
| No metadata | Disables manifest output. |

## History {#history}

Gallery history references files created by CyCapture. Moving or deleting a file externally can leave an unavailable history entry until the Gallery is refreshed.
