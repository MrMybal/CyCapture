---
id: tray
title: Tray, status and notifications
description: Control CyCapture without keeping a window open.
section: Application
order: 90
revision: 1
---

The notification-area icon is the main control surface of CyCapture.

## Tray menu {#menu}

- Open the quick palette or start Image, Video, GIF or Audio directly.
- Open Gallery, Settings, documentation, the capture folder or the plugins folder.
- Toggle plugin quick actions such as CyAnnota Post Edit.
- Stop an active recording.

## Recording state {#recording-state}

```diagram
{"type":"flow","title":"Tray state","nodes":[{"id":"ready","label":"Green · Ready","x":0,"y":50},{"id":"recording","label":"Red · REC + timer","x":300,"y":50},{"id":"saved","label":"Completion notification","x":620,"y":50}],"edges":[{"from":"ready","to":"recording","label":"Start"},{"from":"recording","to":"saved","label":"Stop"},{"from":"saved","to":"ready"}]}
```

The completion notification confirms the output file. Click it within six seconds to open the file directly.

Closing Settings or Gallery does not quit CyCapture. Use **Quit CyCapture** from the tray menu when you want to stop the background process.
