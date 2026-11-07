# CyCapture 1.4.11

**Stage:** Beta<br>
**Platform:** Windows x64<br>
**Release:** [v1.4.11](https://github.com/MrMybal/CyCapture/releases/tag/v1.4.11)

CyCapture is in active beta development. The application is usable for everyday capture workflows, but capture behavior and packaging should still be validated on each target Windows configuration.

## Main changes in this version

- Migrated the integrated help to the latest pinned CyDoc workflow.
- Added hierarchical English, French, and global search, diagrams, media galleries, and code/API references.
- Distributed the complete local documentation reader with the installer, with an embedded fallback for portable use.
- Connected F1, contextual help actions, the README, and GitHub Pages to `Documentation.html`.
- Automated CyDoc generation and GitHub Pages deployment.

## Known limitations

- Official binaries currently target Windows x64 only.
- Windows N and KN editions may require Microsoft's Media Feature Pack for gallery playback.
- Other screenshot utilities can conflict with global Print Screen handling.
- The CyAnnota integration is optional, disabled by default, and does not post-edit GIF captures.
- If an external `Documentation` directory is unavailable, portable builds extract the embedded help into the user's local CyCapture directory.

Report reproducible problems in the [issue tracker](https://github.com/MrMybal/CyCapture/issues).

## Downloads

- [Windows x64 installer](https://github.com/MrMybal/CyCapture/releases/download/v1.4.11/CyCapture-1.4.11-windows-x64-installer.exe)
- [Portable with CyAnnota](https://github.com/MrMybal/CyCapture/releases/download/v1.4.11/CyCapture-1.4.11-windows-x64-portable.exe)
- [Portable without CyAnnota](https://github.com/MrMybal/CyCapture/releases/download/v1.4.11/CyCapture-1.4.11-windows-x64-portable-without-CyAnnota.exe)
- [CyAnnota plugin package](https://github.com/MrMybal/CyCapture/releases/download/v1.4.11/CyCapture.Plugin.CyAnnota-0.4.4.zip)
