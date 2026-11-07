---
id: screen-translation
title: Copy or translate text on screen
description: Recognize text in a selected area, copy it, or display its offline translation over the frozen image.
section: Capture modes
order: 75
revision: 3
---

## Copy text from an area {#copy-text}

Choose **Text** from the quick palette or **Copy text from an area** from the tray menu. Select a visible window, screen, smart element, or free region. CyCapture recognizes the text locally and places the detected lines directly in the clipboard without creating an image file.

The processing window appears in the center of the selected area, on the same monitor. It closes automatically when recognition finishes.

## Translate an area {#translate-area}

Choose **Translation** from the quick palette or **Translate an area** from the tray menu. Select a visible window, screen, smart element, or free region exactly as for an image capture.

CyCapture freezes the selected screen, recognizes the text locally, translates it locally, and draws readable translation boxes over the original lines. The background is never erased or altered.

```diagram
{"type":"flow","title":"Offline screen translation","nodes":[{"id":"select","label":"Select area","x":0,"y":50},{"id":"ocr","label":"Local OCR","x":220,"y":50},{"id":"translate","label":"Local translation","x":450,"y":50},{"id":"overlay","label":"Overlay on frozen screen","x":710,"y":50}],"edges":[{"from":"select","to":"ocr"},{"from":"ocr","to":"translate"},{"from":"translate","to":"overlay"}]}
```

## Languages and models {#models}

Open **Settings > Translation** to choose the OCR source language and the translation target. Automatic source detection initially prepares English, simplified and traditional Chinese, Japanese, and Korean OCR. Selecting a specific source language downloads only its OCR data. The source-language choice is shared by Text and Translation modes.

Use **Manage and download language models** to prepare any supported source languages in advance. Translation directions use Mozilla Bergamot models. If no direct direction exists, CyCapture can use English as a local pivot, for example Chinese → English → French.

Models are downloaded once into the local CyCapture data directory and reused offline. A network connection is needed only for model downloads.

## Reading and copying {#overlay}

- Press **Escape** or click **Close** to leave the translated overlay.
- Click **Copy text** or press **Ctrl+C** to copy all translated lines.
- The **Automatically copy translated text to the clipboard** setting performs that copy as soon as translation completes. It is enabled by default.
- Hovering remains focused on the original screen: the translation is a temporary overlay and no capture file is created.

```callout
{"type":"tip","title":"Chinese OCR","body":"For the most reliable recognition, explicitly select Chinese (simplified) or Chinese (traditional) when you know the script."}
```
