param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$CyAnnotaSource,
    [string]$InnoCompiler = ""
)

$ErrorActionPreference = "Stop"
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$appProject = Join-Path $projectRoot "CyCapture.App\CyCapture.App.csproj"
$pluginProject = Join-Path $projectRoot "CyCapture.Plugin.CyAnnota\CyCapture.Plugin.CyAnnota.csproj"
$installerScript = Join-Path $projectRoot "installer\CyCapture.iss"
$outputRoot = Join-Path $projectRoot "dist-local"
$publishRoot = Join-Path $projectRoot "artifacts\local-publish"
$version = ([xml](Get-Content -LiteralPath $appProject)).Project.PropertyGroup.Version | Select-Object -First 1
$pluginVersion = ([xml](Get-Content -LiteralPath $pluginProject)).Project.PropertyGroup.Version | Select-Object -First 1

if (-not (Test-Path -LiteralPath $CyAnnotaSource -PathType Leaf)) {
    throw "Le binaire CyAnnota local est introuvable : $CyAnnotaSource"
}

function Reset-BuildDirectory([string]$path) {
    $resolved = [System.IO.Path]::GetFullPath($path)
    $projectPrefix = $projectRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($projectPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refus de nettoyer un dossier hors du projet : $resolved"
    }
    if (Test-Path -LiteralPath $resolved) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
    New-Item -ItemType Directory -Path $resolved -Force | Out-Null
}

function Resolve-InnoCompiler([string]$requestedPath) {
    if (-not [string]::IsNullOrWhiteSpace($requestedPath)) {
        $resolved = [System.IO.Path]::GetFullPath($requestedPath)
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
            throw "Le compilateur Inno Setup est introuvable : $resolved"
        }
        return $resolved
    }

    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) {
        $candidates += Join-Path $env:LOCALAPPDATA "Inno Setup 6\ISCC.exe"
    }
    if (-not [string]::IsNullOrWhiteSpace(${env:ProgramFiles(x86)})) {
        $candidates += Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"
    }
    if (-not [string]::IsNullOrWhiteSpace($env:ProgramFiles)) {
        $candidates += Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe"
    }
    $found = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    if ($null -eq $found) {
        throw "Inno Setup 6 est requis pour produire l'installateur. Passez -InnoCompiler avec le chemin de ISCC.exe."
    }
    return [System.IO.Path]::GetFullPath($found)
}

Reset-BuildDirectory $outputRoot
Reset-BuildDirectory $publishRoot
$innoCompilerPath = Resolve-InnoCompiler $InnoCompiler

$fullPublish = Join-Path $publishRoot "with-cyannota"
$litePublish = Join-Path $publishRoot "without-cyannota"

dotnet publish $appProject -c $Configuration -p:Platform=x64 -r $Runtime --self-contained true `
    -p:IncludeCyAnnotaPlugin=true -p:BundleCyAnnota=true -p:CyAnnotaBundleSource=$CyAnnotaSource `
    -o $fullPublish
if ($LASTEXITCODE -ne 0) { throw "La publication complète de CyCapture a échoué." }

dotnet publish $appProject -c $Configuration -p:Platform=x64 -r $Runtime --self-contained true `
    -p:IncludeCyAnnotaPlugin=false -p:BundleCyAnnota=false `
    -o $litePublish
if ($LASTEXITCODE -ne 0) { throw "La publication sans CyAnnota a échoué." }

$fullAsset = Join-Path $outputRoot "CyCapture-$version-windows-x64-portable.exe"
$liteAsset = Join-Path $outputRoot "CyCapture-$version-windows-x64-portable-without-CyAnnota.exe"
Copy-Item -LiteralPath (Join-Path $fullPublish "CyCapture.exe") -Destination $fullAsset -Force
Copy-Item -LiteralPath (Join-Path $litePublish "CyCapture.exe") -Destination $liteAsset -Force

$installerBaseName = "CyCapture-$version-windows-x64-installer"
& $innoCompilerPath `
    "/DAppVersion=$version" `
    "/DSourceDir=$fullPublish" `
    "/DOutputDir=$outputRoot" `
    "/DOutputBaseName=$installerBaseName" `
    $installerScript
if ($LASTEXITCODE -ne 0) { throw "La création de l'installateur CyCapture a échoué." }
$installerAsset = Join-Path $outputRoot "$installerBaseName.exe"

dotnet build $pluginProject -c $Configuration -p:Platform=x64 `
    -p:BundleCyAnnota=true -p:CyAnnotaBundleSource=$CyAnnotaSource
if ($LASTEXITCODE -ne 0) { throw "La construction du plugin CyAnnota a échoué." }

$pluginBuild = Join-Path $projectRoot "CyCapture.Plugin.CyAnnota\bin\x64\$Configuration\net8.0-windows10.0.19041.0\CyCapture.Plugin.CyAnnota.dll"
$pluginAsset = Join-Path $outputRoot "CyCapture.Plugin.CyAnnota-$pluginVersion.dll"
$pluginZip = Join-Path $outputRoot "CyCapture.Plugin.CyAnnota-$pluginVersion.zip"
Copy-Item -LiteralPath $pluginBuild -Destination $pluginAsset -Force
Compress-Archive -LiteralPath @(
    $pluginAsset,
    (Join-Path $projectRoot "CyCapture.Plugin.CyAnnota\README.md"),
    (Join-Path $projectRoot "LICENSE"),
    (Join-Path $projectRoot "BUNDLED_COMPONENTS.md")
) -DestinationPath $pluginZip -Force

Get-Item -LiteralPath $fullAsset, $liteAsset, $installerAsset, $pluginAsset, $pluginZip |
    Select-Object Name, Length, @{ Name = "Sha256"; Expression = { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } }
