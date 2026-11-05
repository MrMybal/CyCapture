---
id: storage
title: Folders, history and metadata
description: Organize captures and choose how technical metadata is stored.
section: Library
order: 100
revision: 1
---

## Folder organization {#folders}

Choose any writable absolute capture folder in Settings.

```diagram
{"type":"architecture","title":"Optional folder layout","nodes":[{"id":"root","label":"CyCapture","x":0,"y":80},{"id":"images","label":"Images","x":260,"y":0},{"id":"videos","label":"Videos","x":260,"y":80},{"id":"gifs","label":"GIFs","x":260,"y":160},{"id":"audio","label":"Audio","x":260,"y":240},{"id":"day","label":"YYYY-MM-DD","x":540,"y":80}],"edges":[{"from":"root","to":"images"},{"from":"root","to":"videos"},{"from":"root","to":"gifs"},{"from":"root","to":"audio"},{"from":"videos","to":"day"}]}
```

- **Separate by type** creates Images, Videos, GIFs and Audio subfolders.
- **Daily folders** add a `YYYY-MM-DD` level.
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
