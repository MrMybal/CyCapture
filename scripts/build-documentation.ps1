param(
    [switch]$ForceRestore
)

$ErrorActionPreference = "Stop"
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$documentationRoot = Join-Path $projectRoot "CyCapture.App\.CyDocBuild"
$outputRoot = Join-Path $projectRoot "CyCapture.App\Documentation"
$cacheRoot = Join-Path $projectRoot "artifacts\npm-cache"
$cydocCommand = Join-Path $documentationRoot "node_modules\.bin\cydoc.cmd"
$vendorRoot = Join-Path $documentationRoot "vendor"
$cydocRepository = "https://github.com/MrMybal/CyDoc.git"
$cydocCommit = "6ddf9a582defb2f11fbb3f992afef6f98dcadc93"
$vendorPackage = Join-Path $vendorRoot "cydoc-0.1.0-$($cydocCommit.Substring(0, 12)).tgz"
$packedVendorPackage = Join-Path $vendorRoot "cydoc-0.1.0.tgz"
$cydocSource = Join-Path $projectRoot "artifacts\cydoc-source-$($cydocCommit.Substring(0, 12))"

$nodeCommand = Get-Command node.exe -ErrorAction SilentlyContinue
$npmCommand = Get-Command npm.cmd -ErrorAction SilentlyContinue
if ($null -eq $nodeCommand -or $null -eq $npmCommand) {
    throw "Node.js 22 or later and npm are required to generate the CyCapture documentation."
}

$nodeVersion = (& $nodeCommand.Source -p "process.versions.node").Trim()
if ($LASTEXITCODE -ne 0 -or [int]($nodeVersion.Split('.')[0]) -lt 22) {
    throw "CyDoc requires Node.js 22 or later. Detected: $nodeVersion"
}

New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null
if ($ForceRestore -or -not (Test-Path -LiteralPath $vendorPackage -PathType Leaf)) {
    $gitCommand = Get-Command git.exe -ErrorAction SilentlyContinue
    if ($null -eq $gitCommand) {
        throw "Git is required to restore the private CyDoc package."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $cydocSource ".git") -PathType Container)) {
        New-Item -ItemType Directory -Force -Path (Split-Path $cydocSource) | Out-Null
        & $gitCommand.Source clone --filter=blob:none --no-checkout $cydocRepository $cydocSource
        if ($LASTEXITCODE -ne 0) { throw "Unable to clone the private CyDoc repository with the available Git authentication." }
    }

    $safeDirectory = $cydocSource.Replace('\', '/')
    & $gitCommand.Source -c "safe.directory=$safeDirectory" -C $cydocSource cat-file -e "$cydocCommit`^{commit}"
    if ($LASTEXITCODE -ne 0) {
        & $gitCommand.Source -c "safe.directory=$safeDirectory" -C $cydocSource fetch --depth 1 origin $cydocCommit
        if ($LASTEXITCODE -ne 0) { throw "Unable to fetch the pinned CyDoc commit $cydocCommit." }
    }
    & $gitCommand.Source -c "safe.directory=$safeDirectory" -C $cydocSource checkout --detach $cydocCommit
    if ($LASTEXITCODE -ne 0) { throw "Unable to check out the pinned CyDoc commit $cydocCommit." }

    Push-Location $cydocSource
    try {
        & $npmCommand.Source ci --no-audit --no-fund --cache $cacheRoot
        if ($LASTEXITCODE -ne 0) { throw "Unable to restore CyDoc build dependencies." }
        New-Item -ItemType Directory -Force -Path $vendorRoot | Out-Null
        & $npmCommand.Source pack --pack-destination $vendorRoot --cache $cacheRoot
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $packedVendorPackage -PathType Leaf)) {
            throw "Unable to create the pinned CyDoc package."
        }
        Move-Item -LiteralPath $packedVendorPackage -Destination $vendorPackage -Force
    }
    finally {
        Pop-Location
    }
}

Push-Location $documentationRoot
try {
    if ($ForceRestore -or -not (Test-Path -LiteralPath $cydocCommand -PathType Leaf)) {
        & $npmCommand.Source ci --no-audit --no-fund --cache $cacheRoot
        if ($LASTEXITCODE -ne 0) { throw "Unable to restore the local CyDoc build dependencies." }
    }

    $resolvedOutput = [System.IO.Path]::GetFullPath($outputRoot)
    $expectedOutput = [System.IO.Path]::GetFullPath((Join-Path $projectRoot "CyCapture.App\Documentation"))
    if (-not $resolvedOutput.Equals($expectedOutput, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean an unexpected documentation output: $resolvedOutput"
    }
    if (Test-Path -LiteralPath $resolvedOutput) {
        Remove-Item -LiteralPath $resolvedOutput -Recurse -Force
    }

    & $cydocCommand check cydoc.config.json
    if ($LASTEXITCODE -ne 0) { throw "CyDoc validation failed." }

    foreach ($required in @(
        "Documentation.html",
        "DocAI.md",
        "DocData\viewer.js",
        "DocData\viewer.css",
        "DocData\data.js",
        "DocData\manifest.json",
        "DocData\translation-report.json",
        "DocData\media-image.js",
        "DocData\media-gallery.js",
        "DocData\assets\cycapture-logo.svg",
        "DocData\assets\licenses\CyDoc-MIT.txt"
    )) {
        if (-not (Test-Path -LiteralPath (Join-Path $resolvedOutput $required) -PathType Leaf)) {
            throw "The CyDoc export is incomplete: $required is missing."
        }
    }

    $unexpectedModules = Get-ChildItem -LiteralPath (Join-Path $resolvedOutput "DocData") -Filter "media-*.js" -File |
        Where-Object { $_.Name -notin @("media-image.js", "media-gallery.js") }
    if ($unexpectedModules) {
        throw "Unexpected CyDoc modules: $($unexpectedModules.Name -join ', ')"
    }

    & $nodeCommand.Source (Join-Path $projectRoot "scripts\verify-documentation.mjs") $resolvedOutput (Join-Path $documentationRoot "cydoc.config.json")
    if ($LASTEXITCODE -ne 0) { throw "The generated documentation failed repository validation." }
}
finally {
    Pop-Location
}

Write-Host "CyCapture documentation generated and validated: $outputRoot"
