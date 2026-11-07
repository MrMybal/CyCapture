---
id: screen-translation
title: Translate text on screen
description: Select text anywhere on the desktop and display its offline translation over the frozen image.
section: Capture modes
order: 75
revision: 2
---

## Translate an area {#translate-area}

Choose **Translation** from the quick palette or **Translate an area** from the tray menu. Select a visible window, screen, smart element, or free region exactly as for an image capture.

CyCapture freezes the selected screen, recognizes the text locally, translates it locally, and draws readable translation boxes over the original lines. The background is never erased or altered.

{"type":"flow","title":"Offline screen translation","nodes":[{"id":"select","label":"Select area","x":0,"y":50},{"id":"ocr","label":"Local OCR","x":220,"y":50},{"id":"translate","label":"Local translation","x":450,"y":50},{"id":"overlay","label":"Overlay on frozen screen","x":710,"y":50}],"edges":[{"from":"select","to":"ocr"},{"from":"ocr","to":"translate"},{"from":"translate","to":"overlay"}]}

## Languages and models {#models}

Open **Settings > Translation** to choose the source and target languages. Automatic source detection initially prepares English, simplified and traditional Chinese, Japanese, and Korean OCR. Selecting a specific source language downloads only its OCR data.

Use **Manage and download language models** to prepare any supported source languages in advance. Translation directions use Mozilla Bergamot models. If no direct direction exists, CyCapture can use English as a local pivot, for example Chinese → English → French.

Models are downloaded once into the local CyCapture data directory and reused offline. A network connection is needed only for model downloads.

## Reading and copying {#overlay}

- Press **Escape** or click **Close** to leave the translated overlay.
- Click **Copy text** or press **Ctrl+C** to copy all translated lines.
- The **Automatically copy translated text to the clipboard** setting performs that copy as soon as translation completes. It is enabled by default.
- Hovering remains focused on the original screen: the translation is a temporary overlay and no capture file is created.

{"type":"tip","title":"Chinese OCR","body":"For the most reliable recognition, explicitly select Chinese (simplified) or Chinese (traditional) when you know the script."}
