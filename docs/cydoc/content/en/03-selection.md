---
id: selection
title: Selecting what to capture
description: Smart targets, windows, monitors and free regions across multiple screens.
section: Capture
order: 30
revision: 1
---

Move the pointer to preview the active target, then click once to confirm it.

```image
{"id":"selection-preview","src":"assets/selection-overlay.svg","alt":"Smart selection across a fictional two-monitor desktop","controls":"minimal"}
```

## Four selection styles {#styles}

- **Smart** detects the visible window, client area and supported controls under the pointer.
- **Window** targets the visible top-level window while respecting Windows Z-order and occlusion.
- **Screen** targets one complete monitor.
- **Free region** lets you drag any rectangle.

```diagram
{"type":"flow","title":"Selection decision","nodes":[{"id":"start","label":"Choose a style","x":0,"y":80},{"id":"smart","label":"Smart target","x":240,"y":0},{"id":"window","label":"Window","x":240,"y":80},{"id":"screen","label":"Screen","x":240,"y":160},{"id":"free","label":"Free region","x":240,"y":240},{"id":"confirm","label":"Preview and confirm","x":520,"y":100}],"edges":[{"from":"start","to":"smart"},{"from":"start","to":"window"},{"from":"start","to":"screen"},{"from":"start","to":"free"},{"from":"smart","to":"confirm"},{"from":"window","to":"confirm"},{"from":"screen","to":"confirm"},{"from":"free","to":"confirm"}]}
```

## Multi-monitor behavior {#multi-monitor}

CyCapture works in the Windows virtual-desktop coordinate space. You can select any connected monitor, including one positioned left of or above the primary display. Per-monitor DPI and offset desktops are accounted for.

Press **Escape** or **Print Screen** to cancel without creating a file.
