param(
    [Parameter(Mandatory = $true)]
    [string] $OutputPath,

    [string] $PngOutputPath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing.Common

function New-LogoBitmap([int] $Size) {
    $bitmap = [Drawing.Bitmap]::new($Size, $Size, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([Drawing.Color]::Transparent)
    $scale = [single]($Size / 1024.0)
    $graphics.ScaleTransform($scale, $scale)
    $dark = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255, 6, 25, 43))
    $white = [Drawing.SolidBrush]::new([Drawing.Color]::White)
    $accent = [Drawing.Pen]::new([Drawing.Color]::FromArgb(255, 120, 255, 50), 54)
    $accent.StartCap = [Drawing.Drawing2D.LineCap]::Round
    $accent.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $monogram = [Drawing.Pen]::new([Drawing.Color]::White, 62)
    $monogram.StartCap = [Drawing.Drawing2D.LineCap]::Flat
    $monogram.EndCap = [Drawing.Drawing2D.LineCap]::Flat

    try {
        $graphics.FillEllipse($dark, 8, 8, 1008, 1008)
        $graphics.DrawArc($accent, 120, 120, 784, 784, 222, 96)
        $graphics.DrawArc($accent, 120, 120, 784, 784, 330, 113)
        $graphics.DrawArc($accent, 120, 120, 784, 784, 97, 113)
        $graphics.DrawArc($monogram, 288, 299, 448, 448, 43, 274)

        [Drawing.PointF[]] $y = @(
            [Drawing.PointF]::new(420, 420),
            [Drawing.PointF]::new(486, 420),
            [Drawing.PointF]::new(512, 478),
            [Drawing.PointF]::new(538, 420),
            [Drawing.PointF]::new(604, 420),
            [Drawing.PointF]::new(545, 530),
            [Drawing.PointF]::new(545, 672),
            [Drawing.PointF]::new(479, 672),
            [Drawing.PointF]::new(479, 530)
        )
        $graphics.FillPolygon($white, $y)
    }
    finally {
        $monogram.Dispose()
        $accent.Dispose()
        $white.Dispose()
        $dark.Dispose()
        $graphics.Dispose()
    }
    return $bitmap
}

function Get-PngBytes([Drawing.Bitmap] $Bitmap) {
    $stream = [IO.MemoryStream]::new()
    try {
        $Bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
        return ,$stream.ToArray()
    }
    finally {
        $stream.Dispose()
    }
}

$target = [IO.Path]::GetFullPath($OutputPath)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null

if (-not [string]::IsNullOrWhiteSpace($PngOutputPath)) {
    $pngTarget = [IO.Path]::GetFullPath($PngOutputPath)
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($pngTarget)) | Out-Null
    $master = New-LogoBitmap 1024
    try {
        $master.Save($pngTarget, [Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $master.Dispose()
    }
}

$entries = foreach ($size in @(16, 20, 24, 32, 40, 48, 64, 128, 256)) {
    $bitmap = New-LogoBitmap $size
    try {
        [pscustomobject]@{ Size = $size; Data = [byte[]](Get-PngBytes $bitmap) }
    }
    finally {
        $bitmap.Dispose()
    }
}

$file = [IO.File]::Create($target)
$writer = [IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$entries.Count)
    $offset = 6 + 16 * $entries.Count
    foreach ($entry in $entries) {
        $writer.Write([byte]($(if ($entry.Size -eq 256) { 0 } else { $entry.Size })))
        $writer.Write([byte]($(if ($entry.Size -eq 256) { 0 } else { $entry.Size })))
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$entry.Data.Length)
        $writer.Write([uint32]$offset)
        $offset += $entry.Data.Length
    }
    foreach ($entry in $entries) {
        $writer.Write([byte[]]$entry.Data)
    }
}
finally {
    $writer.Dispose()
}
