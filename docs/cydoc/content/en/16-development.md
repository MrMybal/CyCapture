---
id: development
title: Build and distribution
description: Build CyCapture, regenerate its local help and create Windows packages.
section: Development
order: 160
revision: 1
---

## Architecture {#architecture}

CyCapture uses C# and Avalonia 12. Interface, models and plugin pipeline are separated from the Windows-specific keyboard hook, window enumeration, capture, clipboard and startup services under `Platform/Windows`.

Windows x64 is the supported target today. The separation is intended to allow future platform implementations; it is not a claim that macOS or Linux capture is already available.

## Requirements {#requirements}

- .NET 8 SDK or newer.
- Windows x64.
- Node.js 22 or newer with npm for CyDoc generation.
- Authenticated Git access to the private CyDoc repository for the first local package restore.
- Inno Setup 6 only when creating the Windows installer.

## Build {#build}

```powershell
git clone https://github.com/MrMybal/CyCapture.git
cd CyCapture
dotnet restore CyCapture.sln --runtime win-x64
dotnet build CyCapture.sln -c Release
```

The .NET build restores the pinned CyDoc commit when needed, then regenerates, validates, archives and embeds the manual. Regenerate only the manual with:

```powershell
./scripts/build-documentation.ps1
```

## Verification {#verification}

```powershell
dotnet run --project ./tests/CyCapture.SettingsChecks/CyCapture.SettingsChecks.csproj -c Release -p:Platform=x64 -- .
```

The checks do not start a real capture or load external plugins. They validate settings, recorder routing, smart-selection Z-order, the embedded CyDoc archive, translations, selected modules and contextual file URLs.

## Windows packages {#packages}

```powershell
./scripts/publish-windows-local.ps1 -CyAnnotaSource "C:\path\to\CyAnnota-portable.exe"
```

The explicit CyAnnota path is required. The script creates the installer, full portable edition, CyAnnota-free portable edition and standalone CyAnnota plugin assets under `dist-local`.
