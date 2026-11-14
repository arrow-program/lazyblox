<#
.SYNOPSIS
  Generates src/LazyBlox/app.ico (PNG-compressed multi-size icon).
.DESCRIPTION
  既定: スクリプト内の描画（アプリ内ロゴと同じデザイン）から app.ico を作ります。
  -ExportPng <path> : 編集用の高画質 PNG（既定 1024x1024、透過）を書き出して終了します。
  -FromPng <path>   : 編集した PNG（正方形・透過推奨）から app.ico を作ります。
.EXAMPLE
  .\scripts\make-icon.ps1 -ExportPng assets\icon.png
  .\scripts\make-icon.ps1 -FromPng assets\icon.png
#>
param(
    [string]$Out = (Join-Path $PSScriptRoot '..\src\LazyBlox\app.ico'),
    [string]$ExportPng,
    [int]$PngSize = 1024,
    [string]$FromPng
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function Resize-Png([Drawing.Image]$src, [int]$size) {
    $bmp = New-Object Drawing.Bitmap $size, $size, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = 'HighQualityBicubic'; $g.PixelOffsetMode = 'HighQuality'; $g.SmoothingMode = 'AntiAlias'
    $g.CompositingQuality = 'HighQuality'; $g.Clear([Drawing.Color]::Transparent)
    $attr = New-Object Drawing.Imaging.ImageAttributes; $attr.SetWrapMode('TileFlipXY')  # no edge bleed
    $g.DrawImage($src, (New-Object Drawing.Rectangle 0, 0, $size, $size), 0, 0, $src.Width, $src.Height, 'Pixel', $attr)
    $g.Dispose()
    $ms = New-Object IO.MemoryStream
    $bmp.Save($ms, [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    , $ms.ToArray()
}

function New-LogoPng([int]$size) {
    $bmp = New-Object Drawing.Bitmap $size, $size, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'; $g.PixelOffsetMode = 'HighQuality'; $g.Clear([Drawing.Color]::Transparent)
    $s = $size / 44.0
    $m = [math]::Max(0.5, $size * 0.02)             # small margin
    $rect = New-Object Drawing.RectangleF $m, $m, ($size - 2 * $m), ($size - 2 * $m)
    $r = $rect.Width * 0.28; $d = $r * 2
    $path = New-Object Drawing.Drawing2D.GraphicsPath
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $brush = New-Object Drawing.Drawing2D.LinearGradientBrush $rect, ([Drawing.Color]::FromArgb(255, 0x3B, 0x82, 0xF6)), ([Drawing.Color]::FromArgb(255, 0x8B, 0x5C, 0xF6)), ([Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
    $g.FillPath($brush, $path)

    $white = [Drawing.Brushes]::White
    if ($size -le 20) {
        # tiny sizes: bigger, centered triangle only
        $pts = [Drawing.PointF[]]@((New-Object Drawing.PointF (15 * $s), (11 * $s)), (New-Object Drawing.PointF (32 * $s), (22 * $s)), (New-Object Drawing.PointF (15 * $s), (33 * $s)))
        $g.FillPolygon($white, $pts)
    } else {
        $pts = [Drawing.PointF[]]@((New-Object Drawing.PointF (18 * $s), (13 * $s)), (New-Object Drawing.PointF (32 * $s), (22 * $s)), (New-Object Drawing.PointF (18 * $s), (31 * $s)))
        $g.FillPolygon($white, $pts)
        $pen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(204, 255, 255, 255)), (2.4 * $s)
        $pen.StartCap = 'Round'; $pen.EndCap = 'Round'
        $g.DrawLine($pen, 10 * $s, 17 * $s, 14 * $s, 17 * $s)
        $g.DrawLine($pen, 8 * $s, 22 * $s, 14 * $s, 22 * $s)
        $g.DrawLine($pen, 10 * $s, 27 * $s, 14 * $s, 27 * $s)
        $pen.Dispose()
    }
    $g.Dispose()
    $ms = New-Object IO.MemoryStream
    $bmp.Save($ms, [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    , $ms.ToArray()
}

if ($ExportPng) {
    $dir = Split-Path $ExportPng
    if ($dir) { New-Item -ItemType Directory -Force $dir | Out-Null }
    $full = Join-Path (Resolve-Path -LiteralPath $(if ($dir) { $dir } else { '.' })) (Split-Path $ExportPng -Leaf)
    [IO.File]::WriteAllBytes($full, (New-LogoPng $PngSize))
    "wrote $full (${PngSize}x${PngSize})"
    return
}

$sizes = 16, 20, 24, 32, 40, 48, 64, 256
if ($FromPng) {
    $src = [Drawing.Image]::FromFile((Resolve-Path -LiteralPath $FromPng).Path)
    try {
        if ($src.Width -ne $src.Height) { Write-Warning "正方形ではありません ($($src.Width)x$($src.Height))。引き伸ばされます。" }
        $images = foreach ($sz in $sizes) { , (Resize-Png $src $sz) }
    } finally { $src.Dispose() }
} else {
    $images = foreach ($sz in $sizes) { , (New-LogoPng $sz) }
}

$fs = [IO.File]::Create((Resolve-Path -LiteralPath (Split-Path $Out) | Join-Path -ChildPath (Split-Path $Out -Leaf)))
$w = New-Object IO.BinaryWriter $fs
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $sz = $sizes[$i]; $len = $images[$i].Length
    $w.Write([byte]($(if ($sz -ge 256) { 0 } else { $sz }))); $w.Write([byte]($(if ($sz -ge 256) { 0 } else { $sz })))
    $w.Write([byte]0); $w.Write([byte]0); $w.Write([uint16]1); $w.Write([uint16]32)
    $w.Write([uint32]$len); $w.Write([uint32]$offset); $offset += $len
}
foreach ($img in $images) { $w.Write($img) }
$w.Close()
"wrote $Out"
