param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$CyAnnotaSource
)

$ErrorActionPreference = "Stop"
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$appProject = Join-Path $projectRoot "CyCapture.App\CyCapture.App.csproj"
$pluginProject = Join-Path $projectRoot "CyCapture.Plugin.CyAnnota\CyCapture.Plugin.CyAnnota.csproj"
$outputRoot = Join-Path $projectRoot "dist-local"
$publishRoot = Join-Path $projectRoot "artifacts\local-publish"
$version = ([xml](Get-Content -LiteralPath $appProject)).Project.PropertyGroup.Version | Select-Object -First 1
$pluginVersion = ([xml](Get-Content -LiteralPath $pluginProject)).Project.PropertyGroup.Version | Select-Object -First 1

if (-not (Test-Path -LiteralPath $CyAnnotaSource -PathType Leaf)) {
    throw "Le binaire CyAnnota local est introuvable : $CyAnnotaSource"
}

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null

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

$fullAsset = Join-Path $outputRoot "CyCapture-$version-windows-x64.exe"
$liteAsset = Join-Path $outputRoot "CyCapture-$version-windows-x64-without-CyAnnota.exe"
Copy-Item -LiteralPath (Join-Path $fullPublish "CyCapture.exe") -Destination $fullAsset -Force
Copy-Item -LiteralPath (Join-Path $litePublish "CyCapture.exe") -Destination $liteAsset -Force

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

Get-Item -LiteralPath $fullAsset, $liteAsset, $pluginAsset, $pluginZip |
    Select-Object Name, Length, @{ Name = "Sha256"; Expression = { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } }
