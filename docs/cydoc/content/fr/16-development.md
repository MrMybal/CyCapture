---
id: development
title: Compilation et distribution
description: Compilez CyCapture, régénérez son aide locale et créez les paquets Windows.
section: Développement
order: 160
sourceRevision: 1
---

## Architecture {#architecture}

CyCapture utilise C# et Avalonia 12. L’interface, les modèles et le pipeline de plugins sont séparés des services Windows : hook clavier, énumération des fenêtres, capture, presse-papiers et démarrage dans `Platform/Windows`.

Windows x64 est la cible prise en charge aujourd’hui. Cette séparation prépare de futures implémentations ; elle ne signifie pas que la capture macOS ou Linux est déjà disponible.

## Prérequis {#requirements}

- SDK .NET 8 ou plus récent.
- Windows x64.
- Node.js 22 ou plus récent avec npm pour générer CyDoc.
- Un accès Git authentifié au dépôt privé CyDoc pour la première restauration du paquet local.
- Inno Setup 6 uniquement pour créer l’installateur Windows.

## Compiler {#build}

```powershell
git clone https://github.com/MrMybal/CyCapture.git
cd CyCapture
dotnet restore CyCapture.sln --runtime win-x64
dotnet build CyCapture.sln -c Release
```

Le build .NET restaure le commit CyDoc épinglé si nécessaire, puis régénère, valide, archive et embarque le manuel. Pour régénérer uniquement le manuel :

```powershell
./scripts/build-documentation.ps1
```

## Vérification {#verification}

```powershell
dotnet run --project ./tests/CyCapture.SettingsChecks/CyCapture.SettingsChecks.csproj -c Release -p:Platform=x64 -- .
```

Les contrôles ne démarrent aucune capture réelle et ne chargent aucun plugin externe. Ils valident les réglages, le routage de l’enregistreur, le Z-order, l’archive CyDoc, les traductions, les modules et les URL contextuelles locales.

## Paquets Windows {#packages}

```powershell
./scripts/publish-windows-local.ps1 -CyAnnotaSource "C:\chemin\vers\CyAnnota-portable.exe"
```

Le chemin explicite de CyAnnota est obligatoire. Le script crée l’installateur, la portable complète, la portable sans CyAnnota et les fichiers du plugin CyAnnota autonome dans `dist-local`.
