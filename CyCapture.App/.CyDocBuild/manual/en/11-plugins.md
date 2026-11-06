---
id: plugins
title: Plugins and CyAnnota
description: Enable post-processing extensions and configure each one independently.
section: Extensibility
order: 110
revision: 2
---

Plugins run after a file is saved. They do not change the selected capture mode.

```diagram
{"type":"flow","title":"Post-processing pipeline","nodes":[{"id":"capture","label":"Saved capture","x":0,"y":50},{"id":"pipeline","label":"Enabled plugins","x":260,"y":50},{"id":"cyannota","label":"Optional CyAnnota","x":520,"y":50},{"id":"result","label":"Result","x":760,"y":50}],"edges":[{"from":"capture","to":"pipeline"},{"from":"pipeline","to":"cyannota"},{"from":"cyannota","to":"result"}]}
```

## Plugin management {#management}

Settings lists each discovered `ICapturePostProcessor`. Enable or disable plugins independently and open configuration only when the plugin exposes settings. Standalone DLLs are loaded from the CyCapture Plugins folder.

## CyAnnota Post Edit {#cyannota}

The full CyCapture edition includes the optional CyAnnota integration. Enable the plugin, then toggle **PostEdit with CyAnnota** in Settings, the tray or the quick palette. On first use, the bundled application is prepared, started and given the completed image or video.

A custom CyAnnota executable can be selected. GIF is skipped until CyAnnota supports it.

## Ordering and failures {#failures}

Enabled processors run sequentially. A plugin error is reported without discarding the original capture. Install extensions only from sources you trust.

Developers can continue with the [plugin API](doc:plugin-api#contract).
