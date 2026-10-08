Add-Type -AssemblyName System.Drawing

$brandDirectory = Join-Path $PSScriptRoot "..\src\MonitorProfiles.App\Resources\Brand"
$brandDirectory = [System.IO.Path]::GetFullPath($brandDirectory)
if (-not (Test-Path -LiteralPath $brandDirectory -PathType Container)) {
    throw "Brand asset directory does not exist: $brandDirectory"
}

function New-RoundedRectanglePath([single]$x, [single]$y, [single]$width, [single]$height, [single]$radius) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $radius * 2
    $path.AddArc($x, $y, $diameter, $diameter, 180, 90)
    $path.AddArc($x + $width - $diameter, $y, $diameter, $diameter, 270, 90)
    $path.AddArc($x + $width - $diameter, $y + $height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($x, $y + $height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-LogoBitmap([int]$size) {
    $supersample = 4
    $renderSize = $size * $supersample
    $bitmap = New-Object System.Drawing.Bitmap($renderSize, $renderSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.ScaleTransform($renderSize / 64.0, $renderSize / 64.0)

    $background = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(0x25, 0x2A, 0x27))
    $accent = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(0xC5, 0xA9, 0x78))
    $screen = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(0x25, 0x2A, 0x27))
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(0xC5, 0xA9, 0x78), 2.6)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    $backgroundPath = New-RoundedRectanglePath 3 3 58 58 15
    $graphics.FillPath($background, $backgroundPath)
    $backgroundPath.Dispose()

    foreach ($bounds in @(@(10, 10, 18, 14), @(36, 10, 18, 14), @(36, 34, 18, 14))) {
        $monitorPath = New-RoundedRectanglePath $bounds[0] $bounds[1] $bounds[2] $bounds[3] 3
        $graphics.DrawPath($pen, $monitorPath)
        $monitorPath.Dispose()
    }

    $portraitPath = New-RoundedRectanglePath 10 33 15 20 3
    $graphics.FillPath($accent, $portraitPath)
    $portraitPath.Dispose()
    $screenPath = New-RoundedRectanglePath 13 36 9 14 1.5
    $graphics.FillPath($screen, $screenPath)
    $screenPath.Dispose()

    $graphics.DrawLine($pen, 19, 24, 19, 28)
    $graphics.DrawLine($pen, 15, 28, 23, 28)
    $graphics.DrawLine($pen, 45, 24, 45, 28)
    $graphics.DrawLine($pen, 41, 28, 49, 28)
    $graphics.DrawLine($pen, 45, 48, 45, 52)
    $graphics.DrawLine($pen, 41, 52, 49, 52)
    $graphics.DrawLine($pen, 17.5, 53, 17.5, 56)
    $graphics.DrawLine($pen, 13.5, 56, 21.5, 56)

    $graphics.ResetTransform()
    $output = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $outputGraphics = [System.Drawing.Graphics]::FromImage($output)
    $outputGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $outputGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $outputGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $outputGraphics.DrawImage($bitmap, 0, 0, $size, $size)

    $background.Dispose()
    $accent.Dispose()
    $screen.Dispose()
    $pen.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
    $outputGraphics.Dispose()
    return $output
}

function Get-PngBytes([int]$size) {
    $bitmap = New-LogoBitmap $size
    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $stream.ToArray()
    $stream.Dispose()
    $bitmap.Dispose()
    return ,$bytes
}

$preview = New-LogoBitmap 256
$preview.Save((Join-Path $brandDirectory "MonitorProfiles.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()

$sizes = @(256, 48, 32, 16)
$pngFrames = foreach ($size in $sizes) { Get-PngBytes $size }
$iconPath = Join-Path $brandDirectory "MonitorProfiles.ico"
$iconStream = [System.IO.File]::Create($iconPath)
$writer = New-Object System.IO.BinaryWriter($iconStream)
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]$sizes.Count)
$imageOffset = 6 + (16 * $sizes.Count)
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $size = $sizes[$i]
    $bytes = $pngFrames[$i]
    $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
    $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$bytes.Length)
    $writer.Write([UInt32]$imageOffset)
    $imageOffset += $bytes.Length
}
foreach ($bytes in $pngFrames) { $writer.Write($bytes) }
$writer.Dispose()
$iconStream.Dispose()

Write-Output "Generated $iconPath and MonitorProfiles.png"
