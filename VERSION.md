# CyCapture 1.5.2

**Stage:** Beta<br>
**Platform:** Windows x64<br>
**Release:** [v1.5.2](https://github.com/MrMybal/CyCapture/releases/tag/v1.5.2)

CyCapture is in active beta development. The application is usable for everyday capture workflows, but capture behavior and packaging should still be validated on each target Windows configuration.

## Main changes in this version

- Added a dedicated Text mode that recognizes a selected area locally and copies the extracted text directly to the clipboard.
- Added Text capture to the quick palette, tray menu, and direct Print Screen behaviors alongside Image, Video, GIF, Audio, and Translation.
- Restored the intended hybrid Print Screen workflow as the default and migrated earlier settings whose stored value could be mistaken for direct Translation mode.
- Centered OCR and translation progress inside the selected area on the correct monitor.
- Fixed the screen-translation diagram and Chinese OCR guidance rendering in the embedded and online CyDoc reader.
- Added fully local on-screen OCR and translation with overlays drawn over the frozen desktop without erasing the original background.
- Added downloadable OCR and Bergamot language models, automatic source detection, explicit simplified/traditional Chinese selection, and local English-pivot translation when needed.
- Added a language-model manager and a Translation mode to the quick palette, tray menu, settings, and direct Print Screen behavior.
- Added independent clipboard options for Image, Video, GIF, Audio, and translated text, enabled by default with one-time migration of earlier preferences.
- Improved audio-only capture with automatic system-level adjustment, corrected MP3 timing, and independent video/audio settings.
- Improved the recording outline, multi-monitor selection, visible-window Z-order handling, startup update prompt, application language selection, and per-mode settings layout.
- Expanded the bilingual offline documentation for translation, models, clipboard behavior, audio, and capture workflows.

## Known limitations

- Official binaries currently target Windows x64 only.
- Windows N and KN editions may require Microsoft's Media Feature Pack for gallery playback.
- Other screenshot utilities can conflict with global Print Screen handling.
- The CyAnnota integration is optional, disabled by default, and does not post-edit GIF captures.
- OCR and translation require a one-time download of the selected language models; recognition quality still depends on the text size, contrast, and selected source language.
- If an external `Documentation` directory is unavailable, portable builds extract the embedded help into the user's local CyCapture directory.

Report reproducible problems in the [issue tracker](https://github.com/MrMybal/CyCapture/issues).

## Downloads

- [Windows x64 installer](https://github.com/MrMybal/CyCapture/releases/download/v1.5.2/CyCapture-1.5.2-windows-x64-installer.exe)
- [Portable with CyAnnota](https://github.com/MrMybal/CyCapture/releases/download/v1.5.2/CyCapture-1.5.2-windows-x64-portable.exe)
- [Portable without CyAnnota](https://github.com/MrMybal/CyCapture/releases/download/v1.5.2/CyCapture-1.5.2-windows-x64-portable-without-CyAnnota.exe)
- [CyAnnota plugin package](https://github.com/MrMybal/CyCapture/releases/download/v1.5.2/CyCapture.Plugin.CyAnnota-0.4.4.zip)
