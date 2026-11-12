Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

# Загрузочная заставка игры (PNG 540x960) в том же стикер-стиле, что generate_icons.ps1:
# толстый коричневый контур, диагональный градиент заливки. Фон — вода пруда,
# по центру улыбающаяся лягушка, снизу надпись «SwampFrog». Файл подключается
# ключом boot_splash/image в project.godot.

$projectRoot = Split-Path -Parent $PSScriptRoot
$outline = [System.Drawing.ColorTranslator]::FromHtml('#2b170f')

function New-Canvas($width, $height) {
$bitmap = New-Object System.Drawing.Bitmap $width, $height
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.Clear([System.Drawing.Color]::Transparent)
return @($bitmap, $graphics)
}

function Save-Image($canvas, $relativePath) {
$path = Join-Path $projectRoot $relativePath
$directory = Split-Path -Parent $path
[void](New-Item -ItemType Directory -Path $directory -Force)
$canvas[0].Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
$canvas[1].Dispose()
$canvas[0].Dispose()
}

function Get-Color($hex) {
return [System.Drawing.ColorTranslator]::FromHtml($hex)
}

function New-Pen($color, $width) {
$pen = New-Object System.Drawing.Pen $color, $width
$pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
return $pen
}

function Draw-Path($g, $path, $colorA, $colorB = $null, $border = 22, $angle = 45) {
if ($null -eq $colorB) { $colorB = $colorA }
$bounds = $path.GetBounds()
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $bounds, $colorA, $colorB, $angle
$g.FillPath($brush, $path)
$brush.Dispose()
if ($border -gt 0) {
$pen = New-Pen $outline $border
$g.DrawPath($pen, $path)
$pen.Dispose()
}
$path.Dispose()
}

function Draw-Ellipse($g, $rect, $colorA, $colorB = $null, $border = 22, $angle = 45) {
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddEllipse($rect[0], $rect[1], $rect[2], $rect[3])
Draw-Path $g $path $colorA $colorB $border $angle
}

function Fill-Ellipse($g, $rect, $color) {
$brush = New-Object System.Drawing.SolidBrush $color
$g.FillEllipse($brush, $rect[0], $rect[1], $rect[2], $rect[3])
$brush.Dispose()
}

# Вертикальный градиент воды: светлее у поверхности, темнее на глубине.
function Fill-Background($g, $width, $height) {
$rect = New-Object System.Drawing.Rectangle 0, 0, $width, $height
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, (Get-Color '#3f9b7e'), (Get-Color '#0b302a'), 90
$g.FillRectangle($brush, $rect)
$brush.Dispose()
}

# Мягкие блики у поверхности воды.
function Draw-WaterShine($g) {
$shine = [System.Drawing.Color]::FromArgb(16, 255, 255, 255)
Fill-Ellipse $g @(-80, 84, 440, 46) $shine
Fill-Ellipse $g @(260, 150, 420, 34) $shine
}

# Лист кувшинки: зелёный эллипс с мягким бликом у верхнего края.
function Draw-LilyPad($g, $cx, $cy, $width, $height) {
$pad = New-Object System.Drawing.Drawing2D.GraphicsPath
$pad.AddEllipse($cx - $width / 2, $cy - $height / 2, $width, $height)
Draw-Path $g $pad (Get-Color '#66c452') (Get-Color '#2f7d24') 20
$gloss = [System.Drawing.Color]::FromArgb(55, 255, 255, 255)
$glossX = $cx - $width * 0.30
$glossY = $cy - $height * 0.30
$glossW = $width * 0.46
$glossH = $height * 0.30
Fill-Ellipse $g @($glossX, $glossY, $glossW, $glossH) $gloss
}

# Улыбающаяся лягушка в стикер-стиле (как на icon.png).
function Draw-Frog($g) {
Draw-Ellipse $g @(70, 250, 400, 360) (Get-Color '#8ad552') (Get-Color '#3d861d') 26
Draw-Ellipse $g @(150, 420, 240, 172) (Get-Color '#f6ffd9') (Get-Color '#dcefa0') 0 90
Fill-Ellipse $g @(104, 424, 74, 48) ([System.Drawing.Color]::FromArgb(150, 246, 150, 150))
Fill-Ellipse $g @(362, 424, 74, 48) ([System.Drawing.Color]::FromArgb(150, 246, 150, 150))
$eyePen = New-Pen $outline 20
$g.DrawArc($eyePen, 152, 300, 96, 96, 180, 180)
$g.DrawArc($eyePen, 292, 300, 96, 96, 180, 180)
$eyePen.Dispose()
}

# Надпись с коричневым контуром и градиентной заливкой.
function Draw-Title($g, $text, $x, $y, $width, $height, $size, $colorA, $colorB, $border = 10) {
try { $family = New-Object System.Drawing.FontFamily 'Arial' }
catch { $family = New-Object System.Drawing.FontFamily 'Microsoft Sans Serif' }
$rect = New-Object System.Drawing.RectangleF $x, $y, $width, $height
$format = New-Object System.Drawing.StringFormat
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddString($text, $family, [int][System.Drawing.FontStyle]::Bold, [float]$size, $rect, $format)
$pen = New-Pen $outline ($border * 2)
$g.DrawPath($pen, $path)
$pen.Dispose()
$bounds = $path.GetBounds()
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $bounds, $colorA, $colorB, 90
$g.FillPath($brush, $path)
$brush.Dispose()
$path.Dispose()
$format.Dispose()
$family.Dispose()
}

function Draw-Splash {
$canvas = New-Canvas 540 960
$g = $canvas[1]
Fill-Background $g 540 960
Draw-WaterShine $g
Draw-LilyPad $g 80 906 262 92
Draw-LilyPad $g 470 900 232 84
Draw-LilyPad $g 300 954 168 60
Draw-Frog $g
Draw-Title $g 'SwampFrog' 20 668 500 150 54 (Get-Color '#fff6c8') (Get-Color '#f2c33d') 12
Save-Image $canvas 'Game/assets/ui/splash.png'
}

Draw-Splash

Write-Host 'Splash image generated: Game/assets/ui/splash.png'
