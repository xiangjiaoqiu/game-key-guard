param([string]$Out = "$PSScriptRoot\app.ico")
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

function New-IcoBytes([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $rect = New-Object System.Drawing.Rectangle(0, 0, ($s - 1), ($s - 1))
    $rad = [Math]::Max(2, [int]($s * 0.235))
    $d = $rad * 2
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc(($rect.Right - $d), 0, $d, $d, 270, 90)
    $path.AddArc(($rect.Right - $d), ($rect.Bottom - $d), $d, $d, 0, 90)
    $path.AddArc(0, ($rect.Bottom - $d), $d, $d, 90, 90)
    $path.CloseFigure()
    $c1 = [System.Drawing.Color]::FromArgb(255, 38, 47, 66)
    $c2 = [System.Drawing.Color]::FromArgb(255, 16, 19, 26)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $c1, $c2, [single]90)
    $g.FillPath($brush, $path)

    $accent = [System.Drawing.Color]::FromArgb(255, 79, 140, 255)
    $penW = [single]([Math]::Max(1.0, $s * 0.085))
    $pen = New-Object System.Drawing.Pen($accent, $penW)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawArc($pen, [single]($s * 0.30), [single]($s * 0.22), [single]($s * 0.40), [single]($s * 0.40), 180, 180)

    $bx = [int]($s * 0.235); $by = [int]($s * 0.45); $bw = [int]($s * 0.53); $bh = [int]($s * 0.37)
    $bodyPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $br = [Math]::Max(2, [int]($s * 0.09)); $bd = $br * 2
    $bodyPath.AddArc($bx, $by, $bd, $bd, 180, 90)
    $bodyPath.AddArc(($bx + $bw - $bd), $by, $bd, $bd, 270, 90)
    $bodyPath.AddArc(($bx + $bw - $bd), ($by + $bh - $bd), $bd, $bd, 0, 90)
    $bodyPath.AddArc($bx, ($by + $bh - $bd), $bd, $bd, 90, 90)
    $bodyPath.CloseFigure()
    $bb = New-Object System.Drawing.SolidBrush($accent)
    $g.FillPath($bb, $bodyPath)

    $hole = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, 16, 19, 26))
    $hr = [single]([Math]::Max(1.0, $s * 0.055))
    $g.FillEllipse($hole, [single]($s * 0.5 - $hr), [single]($s * 0.585), [single]($hr * 2), [single]($hr * 2))

    $g.Dispose(); $pen.Dispose(); $brush.Dispose(); $bb.Dispose(); $hole.Dispose(); $path.Dispose(); $bodyPath.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $bytes = $ms.ToArray()
    $ms.Dispose()
    return , $bytes
}

$sizes = @(16, 32, 48, 64, 128, 256)
$pngs = @()
foreach ($s in $sizes) { $pngs += , (New-IcoBytes $s) }

$fs = [System.IO.File]::Create($Out)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $data = $pngs[$i]
    if ($s -ge 256) { $bw.Write([byte]0) } else { $bw.Write([byte]$s) }
    if ($s -ge 256) { $bw.Write([byte]0) } else { $bw.Write([byte]$s) }
    $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$data.Length); $bw.Write([uint32]$offset)
    $offset += $data.Length
}
foreach ($d in $pngs) { $bw.Write($d) }
$bw.Flush(); $bw.Close(); $fs.Close()
Write-Output ("icon written: " + $Out + " (" + (Get-Item $Out).Length + " bytes)")