# Builds src/WinUtil/Assets/app.ico from app-icon-source.png (edit the PNG to change the icon).
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root "src\WinUtil\Assets\app-icon-source.png"
$out = Join-Path $root "src\WinUtil\Assets\app.ico"
if (-not (Test-Path $src)) {
    Write-Error "Missing $src. Add app-icon-source.png first."
}

$img = [System.Drawing.Bitmap]::FromFile($src)
try {
    $size = 256
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.DrawImage($img, 0, 0, $size, $size)
    $ico = [System.Drawing.Icon]::FromHandle($bmp.GetHicon())
    try {
        $fs = [IO.File]::Create($out)
        $ico.Save($fs)
    }
    finally {
        if ($null -ne $fs) { $fs.Close() }
        $g.Dispose()
        $bmp.Dispose()
    }
}
finally {
    $img.Dispose()
}
Write-Host "Wrote $out"
