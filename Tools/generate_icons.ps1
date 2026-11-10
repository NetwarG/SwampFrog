Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$outline = [System.Drawing.ColorTranslator]::FromHtml('#2b170f')

function New-Canvas {
$bitmap = New-Object System.Drawing.Bitmap 512, 512
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.Clear([System.Drawing.Color]::Transparent)
return @($bitmap, $graphics)
}

function Save-Icon($canvas, $relativePath) {
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

function Draw-Line($g, $from, $to, $color, $width, $border = 0) {
if ($border -gt 0) {
$pen = New-Pen $outline ($width + $border * 2)
$g.DrawLine($pen, $from[0], $from[1], $to[0], $to[1])
$pen.Dispose()
}
$pen = New-Pen $color $width
$g.DrawLine($pen, $from[0], $from[1], $to[0], $to[1])
$pen.Dispose()
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

function New-Polygon($points) {
$converted = New-Object 'System.Collections.Generic.List[System.Drawing.PointF]'
foreach ($point in $points) { $converted.Add([System.Drawing.PointF]::new([float]$point[0], [float]$point[1])) }
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddPolygon($converted.ToArray())
return $path
}

function New-Star($cx, $cy, $outer, $inner, $rays) {
$points = New-Object 'System.Collections.Generic.List[System.Drawing.PointF]'
for ($i = 0; $i -lt $rays * 2; $i++) {
$radius = if ($i % 2 -eq 0) { $outer } else { $inner }
$angle = -([Math]::PI / 2) + $i * [Math]::PI / $rays
$points.Add([System.Drawing.PointF]::new([float]($cx + [Math]::Cos($angle) * $radius), [float]($cy + [Math]::Sin($angle) * $radius)))
}
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddPolygon($points.ToArray())
return $path
}

function New-Heart($cx, $cy, $size) {
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.StartFigure()
$path.AddBezier($cx, $cy + $size * 0.43, $cx - $size * 0.55, $cy + $size * 0.08, $cx - $size * 0.53, $cy - $size * 0.32, $cx - $size * 0.25, $cy - $size * 0.39)
$path.AddBezier($cx - $size * 0.25, $cy - $size * 0.39, $cx - $size * 0.08, $cy - $size * 0.44, $cx, $cy - $size * 0.30, $cx, $cy - $size * 0.22)
$path.AddBezier($cx, $cy - $size * 0.22, $cx, $cy - $size * 0.30, $cx + $size * 0.08, $cy - $size * 0.44, $cx + $size * 0.25, $cy - $size * 0.39)
$path.AddBezier($cx + $size * 0.25, $cy - $size * 0.39, $cx + $size * 0.53, $cy - $size * 0.32, $cx + $size * 0.55, $cy + $size * 0.08, $cx, $cy + $size * 0.43)
$path.CloseFigure()
return $path
}

function New-Apple($cx, $cy, $size) {
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.StartFigure()
$path.AddBezier($cx, $cy - $size * 0.28, $cx - $size * 0.18, $cy - $size * 0.46, $cx - $size * 0.50, $cy - $size * 0.34, $cx - $size * 0.46, $cy + $size * 0.02)
$path.AddBezier($cx - $size * 0.46, $cy + $size * 0.02, $cx - $size * 0.43, $cy + $size * 0.34, $cx - $size * 0.12, $cy + $size * 0.48, $cx, $cy + $size * 0.39)
$path.AddBezier($cx, $cy + $size * 0.39, $cx + $size * 0.12, $cy + $size * 0.48, $cx + $size * 0.43, $cy + $size * 0.34, $cx + $size * 0.46, $cy + $size * 0.02)
$path.AddBezier($cx + $size * 0.46, $cy + $size * 0.02, $cx + $size * 0.50, $cy - $size * 0.34, $cx + $size * 0.18, $cy - $size * 0.46, $cx, $cy - $size * 0.28)
$path.CloseFigure()
return $path
}

function Draw-Apple($g, $cx, $cy, $size, $colorA, $colorB, $border = 22) {
Draw-Line $g @($cx, ($cy - $size * 0.28)) @(($cx + $size * 0.06), ($cy - $size * 0.53)) (Get-Color '#805022') ($size * 0.075) 8
$leaf = New-Object System.Drawing.Drawing2D.GraphicsPath
$leaf.AddBezier($cx + $size * 0.02, $cy - $size * 0.40, $cx + $size * 0.16, $cy - $size * 0.62, $cx + $size * 0.42, $cy - $size * 0.58, $cx + $size * 0.38, $cy - $size * 0.39)
$leaf.AddBezier($cx + $size * 0.38, $cy - $size * 0.39, $cx + $size * 0.23, $cy - $size * 0.26, $cx + $size * 0.02, $cy - $size * 0.40, $cx + $size * 0.02, $cy - $size * 0.40)
$leaf.CloseFigure()
Draw-Path $g $leaf (Get-Color '#9ad52f') (Get-Color '#3d861d') ($border * 0.65) 30
Draw-Path $g (New-Apple $cx $cy $size) $colorA $colorB $border 35
Fill-Ellipse $g @(($cx - $size * 0.26), ($cy - $size * 0.19), ($size * 0.12), ($size * 0.20)) ([System.Drawing.Color]::FromArgb(225, 255, 255, 255))
Fill-Ellipse $g @(($cx - $size * 0.28), ($cy + $size * 0.02), ($size * 0.055), ($size * 0.065)) ([System.Drawing.Color]::FromArgb(190, 255, 255, 255))
}

function New-Pear($cx, $cy, $size) {
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.StartFigure()
# Шейка узкая у плодоножки, тело расширяется книзу — силуэт груши.
$path.AddBezier($cx, $cy - $size * 0.36, $cx - $size * 0.12, $cy - $size * 0.34, $cx - $size * 0.18, $cy - $size * 0.12, $cx - $size * 0.24, $cy + $size * 0.04)
$path.AddBezier($cx - $size * 0.24, $cy + $size * 0.04, $cx - $size * 0.38, $cy + $size * 0.18, $cx - $size * 0.34, $cy + $size * 0.44, $cx, $cy + $size * 0.50)
$path.AddBezier($cx, $cy + $size * 0.50, $cx + $size * 0.34, $cy + $size * 0.44, $cx + $size * 0.38, $cy + $size * 0.18, $cx + $size * 0.24, $cy + $size * 0.04)
$path.AddBezier($cx + $size * 0.24, $cy + $size * 0.04, $cx + $size * 0.18, $cy - $size * 0.12, $cx + $size * 0.12, $cy - $size * 0.34, $cx, $cy - $size * 0.36)
$path.CloseFigure()
return $path
}

function Draw-Pear($g, $cx, $cy, $size, $colorA, $colorB, $border = 22) {
Draw-Line $g @($cx, ($cy - $size * 0.36)) @(($cx + $size * 0.05), ($cy - $size * 0.60)) (Get-Color '#7a5228') ($size * 0.07) 8
$leaf = New-Object System.Drawing.Drawing2D.GraphicsPath
$leaf.AddBezier($cx + $size * 0.02, $cy - $size * 0.48, $cx + $size * 0.16, $cy - $size * 0.68, $cx + $size * 0.40, $cy - $size * 0.64, $cx + $size * 0.36, $cy - $size * 0.45)
$leaf.AddBezier($cx + $size * 0.36, $cy - $size * 0.45, $cx + $size * 0.22, $cy - $size * 0.34, $cx + $size * 0.02, $cy - $size * 0.48, $cx + $size * 0.02, $cy - $size * 0.48)
$leaf.CloseFigure()
Draw-Path $g $leaf (Get-Color '#9ad52f') (Get-Color '#3d861d') ($border * 0.65) 30
Draw-Path $g (New-Pear $cx $cy $size) $colorA $colorB $border 40
Fill-Ellipse $g @(($cx - $size * 0.15), ($cy - $size * 0.16), ($size * 0.10), ($size * 0.16)) ([System.Drawing.Color]::FromArgb(220, 255, 255, 255))
Fill-Ellipse $g @(($cx - $size * 0.26), ($cy + $size * 0.20), ($size * 0.055), ($size * 0.08)) ([System.Drawing.Color]::FromArgb(185, 255, 255, 255))
}

function Draw-Heart($g, $cx, $cy, $size, $cross) {
Draw-Path $g (New-Heart $cx $cy $size) (Get-Color '#ff6675') (Get-Color '#c92347') 26 40
Fill-Ellipse $g @(($cx - $size * 0.27), ($cy - $size * 0.23), ($size * 0.12), ($size * 0.16)) ([System.Drawing.Color]::FromArgb(230, 255, 255, 255))
if ($cross) {
$u = $size * 0.09
$points = @(
@(($cx - $u), ($cy - 3 * $u)), @(($cx + $u), ($cy - 3 * $u)),
@(($cx + $u), ($cy - $u)), @(($cx + 3 * $u), ($cy - $u)),
@(($cx + 3 * $u), ($cy + $u)), @(($cx + $u), ($cy + $u)),
@(($cx + $u), ($cy + 3 * $u)), @(($cx - $u), ($cy + 3 * $u)),
@(($cx - $u), ($cy + $u)), @(($cx - 3 * $u), ($cy + $u)),
@(($cx - 3 * $u), ($cy - $u)), @(($cx - $u), ($cy - $u))
)
Draw-Path $g (New-Polygon $points) (Get-Color '#ffffff') (Get-Color '#e9fff6') 12 45
}
}

# Лапа лягушки одним силуэтом (как у жабы в игре): ладонь-овал, запястье и три пальца
# с подушечками. $size — вылет от центра ладони до кончика пальца.
function Draw-FrogHand($g, $cx, $cy, $angle, $size, $color, $light) {
$state = $g.Save()
$g.TranslateTransform($cx, $cy)
$g.RotateTransform($angle)
$palmW = $size * 0.44
$palmH = $size * 0.38
$pad = $size * 0.20
$toeD = $size * 0.78
$toeW = 14
$palmRect = @((- $palmW), (- $palmH), ($palmW * 2), ($palmH * 2))
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.StartFigure()
# Ладонь: нижняя кромка и левая сторона до первого пальца (углы GDI+ растут по часовой).
$path.AddArc($palmRect[0], $palmRect[1], $palmRect[2], $palmRect[3], 60, 60)
$path.AddArc($palmRect[0], $palmRect[1], $palmRect[2], $palmRect[3], 120, 94)
$toes = @(228, 270, 312)
for ($i = 0; $i -lt $toes.Length; $i++) {
$toe = $toes[$i]
if ($i -gt 0) {
# Перемычка между пальцами: вогнутая дуга к центру ладони.
$from = $toes[$i - 1] + $toeW
$to = $toe - $toeW
$a1 = Get-CirclePoint 0 0 1 $from
$a4 = Get-CirclePoint 0 0 1 $to
$c1 = Get-CirclePoint 0 0 ($palmW * 0.45) ($from + 4)
$c2 = Get-CirclePoint 0 0 ($palmW * 0.45) ($to - 4)
$path.AddBezier(($a1[0] * $palmW), ($a1[1] * $palmH), $c1[0], $c1[1], $c2[0], $c2[1], ($a4[0] * $palmW), ($a4[1] * $palmH))
}
$padCenter = Get-CirclePoint 0 0 $toeD $toe
$path.AddArc(($padCenter[0] - $pad), ($padCenter[1] - $pad), ($pad * 2), ($pad * 2), ($toe - 90), 180)
}
# Правая кромка ладони и замыкание контура (с 326° — от основания правого пальца).
$path.AddArc($palmRect[0], $palmRect[1], $palmRect[2], $palmRect[3], 326, 94)
$path.CloseFigure()
Draw-Path $g $path $color $light ($size * 0.075) 55
# Блики: мягкое пятно на ладони и точка на средней подушечке.
Fill-Ellipse $g @((-$palmW * 0.55), (-$palmH * 0.78), ($palmW * 0.44), ($palmH * 0.52)) ([System.Drawing.Color]::FromArgb(135, 255, 255, 255))
$midPad = Get-CirclePoint 0 0 $toeD 270
Fill-Ellipse $g @(($midPad[0] - $pad * 0.72), ($midPad[1] - $pad * 0.82), ($pad * 0.46), ($pad * 0.46)) ([System.Drawing.Color]::FromArgb(120, 255, 255, 255))
$g.Restore($state)
}

# Мусорная банка в стиле падающего мусора: тело, крышка, ручка, планка и рёбра.
function Draw-TrashCan($g, $cx, $cy, $scale, $angle) {
$state = $g.Save()
$g.TranslateTransform($cx, $cy)
$g.RotateTransform($angle)
$body = @(@((-87 * $scale), (-60 * $scale)), @((87 * $scale), (-60 * $scale)), @((61 * $scale), (172 * $scale)), @((-65 * $scale), (172 * $scale)))
Draw-Path $g (New-Polygon $body) (Get-Color '#b8c8c5') (Get-Color '#65727f') (24 * $scale) 35
Draw-Line $g @((-110 * $scale), (-70 * $scale)) @((110 * $scale), (-70 * $scale)) (Get-Color '#9aa9ad') (52 * $scale) (13 * $scale)
Draw-Line $g @((-32 * $scale), (-124 * $scale)) @((32 * $scale), (-124 * $scale)) (Get-Color '#9aa9ad') (34 * $scale) (12 * $scale)
Draw-Line $g @((-45 * $scale), (-45 * $scale)) @((-34 * $scale), (110 * $scale)) (Get-Color '#4e5b65') (18 * $scale) 0
Draw-Line $g @(0, (-45 * $scale)) @(0, (110 * $scale)) (Get-Color '#4e5b65') (18 * $scale) 0
Draw-Line $g @((45 * $scale), (-45 * $scale)) @((34 * $scale), (110 * $scale)) (Get-Color '#4e5b65') (18 * $scale) 0
Draw-Line $g @((-97 * $scale), (-45 * $scale)) @((97 * $scale), (-45 * $scale)) (Get-Color '#d9e2df') (13 * $scale) 0
$g.Restore($state)
}

# Наконечник стрелки: база уходит назад за конец дуги, острие — по касательной.
function New-ArcArrowHead($rect, $degrees, $length, $halfWidth) {
$radius = $rect[2] / 2.0
$cx = $rect[0] + $radius
$cy = $rect[1] + $rect[3] / 2.0
$point = Get-CirclePoint $cx $cy $radius $degrees
$dirX = -($point[1] - $cy)
$dirY = $point[0] - $cx
$norm = [Math]::Sqrt($dirX * $dirX + $dirY * $dirY)
if ($norm -lt 0.001) { $norm = 1 }
$dirX = $dirX / $norm
$dirY = $dirY / $norm
$backX = $point[0] - $dirX * $length * 0.45
$backY = $point[1] - $dirY * $length * 0.45
$tipX = $point[0] + $dirX * $length
$tipY = $point[1] + $dirY * $length
$sideX = -$dirY
$sideY = $dirX
return @(@(($backX + $sideX * $halfWidth), ($backY + $sideY * $halfWidth)), @($tipX, $tipY), @(($backX - $sideX * $halfWidth), ($backY - $sideY * $halfWidth)))
}

function Draw-ArcArrow($g, $rect, $start, $sweep, $color, $width) {
$pen = New-Pen $outline ($width + 18)
$g.DrawArc($pen, $rect[0], $rect[1], $rect[2], $rect[3], $start, $sweep)
$pen.Dispose()
$pen = New-Pen $color $width
$g.DrawArc($pen, $rect[0], $rect[1], $rect[2], $rect[3], $start, $sweep)
$pen.Dispose()
Draw-Path $g (New-Polygon (New-ArcArrowHead $rect ($start + $sweep) ($width * 1.9) $width)) $color $color 14
}

# Заливка и мягкая обводка произвольного цвета: для фона коричневый контур иконок не подходит.
function Draw-SoftPath($g, $path, $colorA, $colorB, $stroke, $width, $angle = 45) {
$bounds = $path.GetBounds()
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $bounds, $colorA, $colorB, $angle
$g.FillPath($brush, $path)
$brush.Dispose()
if ($null -ne $stroke -and $width -gt 0) {
$pen = New-Pen $stroke $width
$g.DrawPath($pen, $path)
$pen.Dispose()
}
$path.Dispose()
}

function Draw-SoftEllipse($g, $rect, $colorA, $colorB, $stroke, $width, $angle = 45) {
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddEllipse($rect[0], $rect[1], $rect[2], $rect[3])
Draw-SoftPath $g $path $colorA $colorB $stroke $width $angle
}

function Get-CirclePoint($cx, $cy, $radius, $degrees) {
$angle = $degrees * [Math]::PI / 180
return @([float]($cx + [Math]::Cos($angle) * $radius), [float]($cy + [Math]::Sin($angle) * $radius))
}

# Диск с вырезом-«пакманом»: так лист кувшинки читается сверху.
function New-LilyPadPath($cx, $cy, $radius, $notch, $notchAngle) {
$half = $notch / 2.0
$first = $notchAngle - $half
$second = $notchAngle + $half
$start = Get-CirclePoint $cx $cy $radius $second
$end = Get-CirclePoint $cx $cy $radius $first
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.StartFigure()
# От кромки выреза через центр, дуга по телу листа (360 − вырез) и обратно в центр.
$path.AddLine([float]$cx, [float]$cy, $start[0], $start[1])
$path.AddArc([float]($cx - $radius), [float]($cy - $radius), [float]($radius * 2), [float]($radius * 2), [float]$second, [float](360 - $notch))
$path.AddLine($end[0], $end[1], [float]$cx, [float]$cy)
$path.CloseFigure()
return $path
}

# Фон пруда: лист кувшинки приглушённых тонов, без коричневого контура иконок.
function Draw-LilyPad {
$canvas = New-Canvas; $g = $canvas[1]
$cx = 256; $cy = 256; $radius = 214
$notch = 30; $notchAngle = 90
Draw-SoftPath $g (New-LilyPadPath $cx $cy $radius $notch $notchAngle) (Get-Color '#2f6b50') (Get-Color '#17402f') (Get-Color '#14392a') 8 135
for ($i = 0; $i -lt 8; $i++) {
$from = Get-CirclePoint $cx $cy ($radius * 0.20) ($notchAngle + 22.5 + $i * 45)
$to = Get-CirclePoint $cx $cy ($radius * 0.78) ($notchAngle + 22.5 + $i * 45)
Draw-Line $g $from $to (Get-Color '#3c7d5e') 7 0
}
Fill-Ellipse $g @(($cx - $radius * 0.52), ($cy - $radius * 0.60), ($radius * 0.30), ($radius * 0.42)) ([System.Drawing.Color]::FromArgb(40, 255, 255, 255))
Fill-Ellipse $g @(($cx - $radius * 0.36), ($cy - $radius * 0.12), ($radius * 0.10), ($radius * 0.12)) ([System.Drawing.Color]::FromArgb(28, 255, 255, 255))
Save-Icon $canvas 'Game/assets/ui/lily_pad.png'
}

# Фон пруда: цветок кувшинки — бледные лепестки, тоже без контура иконок.
function Draw-LilyFlower {
$canvas = New-Canvas; $g = $canvas[1]
$cx = 256; $cy = 256
for ($i = 0; $i -lt 8; $i++) {
$state = $g.Save()
$g.TranslateTransform($cx, $cy)
$g.RotateTransform($i * 45)
Draw-SoftEllipse $g @(-74, -218, 148, 190) (Get-Color '#ffffff') (Get-Color '#ffe6ef') (Get-Color '#f2c3d4') 11 60
$g.Restore($state)
}
for ($i = 0; $i -lt 6; $i++) {
$state = $g.Save()
$g.TranslateTransform($cx, $cy)
$g.RotateTransform($i * 60 + 30)
Draw-SoftEllipse $g @(-56, -178, 112, 152) (Get-Color '#ffe7f0') (Get-Color '#f7b6cd') (Get-Color '#e79cb6') 10 90
$g.Restore($state)
}
Draw-SoftEllipse $g @(156, 156, 200, 200) (Get-Color '#ffe066') (Get-Color '#e39b18') (Get-Color '#c8871a') 12 45
for ($i = 0; $i -lt 12; $i++) {
$angle = $i * [Math]::PI / 6
Fill-Ellipse $g @(($cx + [Math]::Cos($angle) * 54 - 10), ($cy + [Math]::Sin($angle) * 54 - 10), 20, 20) (Get-Color '#d99a1c')
}
Save-Icon $canvas 'Game/assets/ui/lily_flower.png'
}


function Draw-HealthIcons {
$canvas = New-Canvas
Draw-Heart $canvas[1] 256 270 440 $false
Save-Icon $canvas 'Game/assets/ui/hp.png'
$canvas = New-Canvas
Draw-Heart $canvas[1] 256 270 440 $true
Save-Icon $canvas 'Game/assets/fruits/healing.png'
}

function Draw-StickyPaws {
$canvas = New-Canvas; $g = $canvas[1]
$green = Get-Color '#79c832'; $light = Get-Color '#b4e863'
# Лапа лягушки с подушечками тянется к яблоку: перк увеличивает радиус ловли.
Draw-FrogHand $g 222 288 -6 240 $green $light
Draw-Apple $g 392 176 146 (Get-Color '#ff5548') (Get-Color '#cf2438') 18
Save-Icon $canvas 'Game/assets/perks/sticky_paws.png'
}

function Draw-RubberGrip {
$canvas = New-Canvas; $g = $canvas[1]
$green = Get-Color '#79c832'; $light = Get-Color '#b4e863'; $spark = Get-Color '#ffd45e'
# Лапа отбивает мусорную банку: дуга отскока над ней и искры в точке удара.
Draw-ArcArrow $g @(170, 60, 290, 290) 150 175 $spark 38
Draw-TrashCan $g 364 238 0.48 24
Draw-Line $g @(252, 276) @(288, 250) $spark 14 6
Draw-Line $g @(266, 322) @(304, 298) $spark 14 6
Draw-Line $g @(236, 238) @(268, 214) $spark 14 6
Draw-FrogHand $g 132 344 24 186 $green $light
Save-Icon $canvas 'Game/assets/perks/rubber_grip.png'
}

function Draw-RapidFire {
$canvas = New-Canvas; $g = $canvas[1]; $green = Get-Color '#91d844'; $light = Get-Color '#b4e863'
# Полоски скорости: сверху самая длинная, ниже всё короче.
Draw-Line $g @(30, 132) @(196, 132) (Get-Color '#ff8c7a') 28 9
Draw-Line $g @(30, 224) @(150, 224) (Get-Color '#ff8c7a') 26 9
Draw-Line $g @(30, 316) @(104, 316) (Get-Color '#ff8c7a') 24 9
# Лапа на вытянутой руке: рука сужается к запястью и изгибается, как у жабы в игре.
$shoulderX = 96; $shoulderY = 408; $armW1 = 46
$wristX = 252; $wristY = 314; $armW2 = 31
$axisDeg = [Math]::Atan2(($wristY - $shoulderY), ($wristX - $shoulderX)) * 180.0 / [Math]::PI
$side = Get-CirclePoint 0 0 1 ($axisDeg + 90)
$s1x = $shoulderX + $side[0] * $armW1; $s1y = $shoulderY + $side[1] * $armW1
$s2x = $shoulderX - $side[0] * $armW1; $s2y = $shoulderY - $side[1] * $armW1
$w1x = $wristX + $side[0] * $armW2; $w1y = $wristY + $side[1] * $armW2
$w2x = $wristX - $side[0] * $armW2; $w2y = $wristY - $side[1] * $armW2
$arm = New-Object System.Drawing.Drawing2D.GraphicsPath
$arm.StartFigure()
$arm.AddArc(($shoulderX - $armW1), ($shoulderY - $armW1), ($armW1 * 2), ($armW1 * 2), ($axisDeg + 90), 180)
$arm.AddBezier([float]$s2x, [float]$s2y, [float]($s2x - $side[0] * 22), [float]($s2y - $side[1] * 22), [float]($w2x - $side[0] * 22), [float]($w2y - $side[1] * 22), [float]$w2x, [float]$w2y)
$arm.AddArc(($wristX - $armW2), ($wristY - $armW2), ($armW2 * 2), ($armW2 * 2), ($axisDeg - 90), 180)
$arm.AddBezier([float]$w1x, [float]$w1y, [float]($w1x - $side[0] * 22), [float]($w1y - $side[1] * 22), [float]($s1x - $side[0] * 22), [float]($s1y - $side[1] * 22), [float]$s1x, [float]$s1y)
$arm.CloseFigure()
Draw-SoftPath $g $arm $green (Get-Color '#68b02c') $outline 13 45
Draw-FrogHand $g 318 236 38 174 $green $light
Save-Icon $canvas 'Game/assets/perks/rapid_fire.png'
}

function Draw-SlowZone {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Ellipse $g @(54, 45, 368, 368) (Get-Color '#9af0df') (Get-Color '#379b91') 26 40
Draw-Ellipse $g @(94, 85, 288, 288) (Get-Color '#d8fff4') (Get-Color '#78cfc1') 10 40
Draw-Line $g @(238, 105) @(238, 133) $outline 14 0
Draw-Line $g @(238, 343) @(238, 371) $outline 14 0
Draw-Line $g @(104, 229) @(132, 229) $outline 14 0
Draw-Line $g @(344, 229) @(372, 229) $outline 14 0
Draw-Line $g @(238, 229) @(238, 137) (Get-Color '#285c55') 22 5
Draw-Line $g @(238, 229) @(310, 279) (Get-Color '#285c55') 22 5
Draw-Ellipse $g @(219, 210, 38, 38) (Get-Color '#ffd45e') (Get-Color '#f29d32') 8 45
Draw-Apple $g 377 381 142 (Get-Color '#ff6555') (Get-Color '#cf2940') 18
Save-Icon $canvas 'Game/assets/perks/slow_zone.png'
}

function Draw-GoldRush {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Apple $g 248 290 370 (Get-Color '#fff076') (Get-Color '#eaa313') 26
Draw-Path $g (New-Star 410 102 65 25 4) (Get-Color '#fff9ae') (Get-Color '#ffd32e') 14 45
Draw-Path $g (New-Star 88 140 40 15 4) (Get-Color '#fff9ae') (Get-Color '#ffd32e') 10 45
Save-Icon $canvas 'Game/assets/perks/gold_rush.png'
}

function Draw-FruitExplosion {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Path $g (New-Star 256 270 231 153 12) (Get-Color '#ffd45e') (Get-Color '#ff792f') 22 35
Draw-Apple $g 250 285 258 (Get-Color '#ff6650') (Get-Color '#d52d38') 22
Draw-Ellipse $g @(54, 62, 61, 61) (Get-Color '#ffe76a') (Get-Color '#f39b25') 13 45
Draw-Ellipse $g @(390, 67, 55, 55) (Get-Color '#ffe76a') (Get-Color '#f39b25') 13 45
Draw-Ellipse $g @(402, 401, 50, 50) (Get-Color '#ffe76a') (Get-Color '#f39b25') 12 45
Save-Icon $canvas 'Game/assets/perks/fruit_explosion.png'
}

function Draw-ComboMaster {
$canvas = New-Canvas; $g = $canvas[1]
# Стрелка роста серии: по ней летят два фрукта, множитель растёт.
Draw-Line $g @(92, 404) @(403, 94) (Get-Color '#ffd45e') 31 11
Draw-Path $g (New-Polygon @(@(348, 64), @(455, 48), @(438, 154))) (Get-Color '#ffe76a') (Get-Color '#f0a62b') 16 45
$state = $g.Save()
$g.TranslateTransform(140, 356)
$g.RotateTransform(-8)
Draw-Apple $g 0 0 124 (Get-Color '#ff5548') (Get-Color '#cf2438') 17
$g.Restore($state)
$state = $g.Save()
$g.TranslateTransform(286, 210)
$g.RotateTransform(10)
Draw-Pear $g 0 0 130 (Get-Color '#e2e86a') (Get-Color '#9fbc2a') 17
$g.Restore($state)
Save-Icon $canvas 'Game/assets/perks/combo_master.png'
}

function Draw-Shell {
$canvas = New-Canvas; $g = $canvas[1]
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddBezier(61, 355, 70, 160, 160, 66, 256, 66)
$path.AddBezier(256, 66, 352, 66, 442, 160, 451, 355)
$path.AddLine(451, 355, 61, 355)
$path.CloseFigure()
Draw-Path $g $path (Get-Color '#b4e863') (Get-Color '#438c36') 26 40
Draw-Path $g (New-Polygon @(@(256, 159), @(331, 202), @(331, 288), @(256, 331), @(181, 288), @(181, 202))) (Get-Color '#8fc94b') (Get-Color '#5d9c39') 15 45
Draw-Line $g @(256, 72) @(256, 157) (Get-Color '#396f2e') 15 0
Draw-Line $g @(103, 207) @(179, 218) (Get-Color '#396f2e') 15 0
Draw-Line $g @(409, 207) @(333, 218) (Get-Color '#396f2e') 15 0
Draw-Line $g @(138, 350) @(195, 293) (Get-Color '#396f2e') 15 0
Draw-Line $g @(374, 350) @(317, 293) (Get-Color '#396f2e') 15 0
Draw-Line $g @(55, 367) @(457, 367) (Get-Color '#78b943') 52 13
Fill-Ellipse $g @(127, 137, 50, 64) ([System.Drawing.Color]::FromArgb(125, 255, 255, 255))
Save-Icon $canvas 'Game/assets/perks/shell.png'
}

function Draw-TrashBounce {
$canvas = New-Canvas; $g = $canvas[1]
# Дуга отскока идёт вокруг банки, наконечник встаёт на её конце.
Draw-ArcArrow $g @(62, 58, 372, 372) 150 178 (Get-Color '#7fe3d5') 38
Draw-TrashCan $g 254 292 0.86 0
Save-Icon $canvas 'Game/assets/perks/trash_bounce.png'
}

function Draw-SecondLife {
$canvas = New-Canvas; $g = $canvas[1]
Draw-ArcArrow $g @(59, 60, 394, 394) 38 282 (Get-Color '#b4e863') 43
Draw-Heart $g 256 283 304 $false
Save-Icon $canvas 'Game/assets/perks/second_life.png'
}

function Draw-LightningTongue {
$canvas = New-Canvas; $g = $canvas[1]
# Только молния: язык из иконки убран, разряд занимает весь кадр.
$bolt = @(@(292, 35), @(111, 287), @(233, 270), @(183, 474), @(414, 188), @(286, 207))
Draw-Path $g (New-Polygon $bolt) (Get-Color '#ffb2c2') (Get-Color '#ef4677') 25 35
Draw-Line $g @(275, 102) @(203, 208) (Get-Color '#ffe5ed') 20 0
Save-Icon $canvas 'Game/assets/perks/lightning_tongue.png'
}

function Draw-BasiliskGaze {
$canvas = New-Canvas; $g = $canvas[1]
$eye = New-Object System.Drawing.Drawing2D.GraphicsPath
$eye.AddBezier(43, 256, 125, 93, 381, 93, 469, 256)
$eye.AddBezier(469, 256, 381, 419, 125, 419, 43, 256)
$eye.CloseFigure()
Draw-Path $g $eye (Get-Color '#ffffff') (Get-Color '#c8eadb') 26 35
Draw-Ellipse $g @(174, 139, 170, 231) (Get-Color '#7fe3d5') (Get-Color '#238c85') 19 40
Draw-Ellipse $g @(217, 170, 82, 166) (Get-Color '#152d2b') (Get-Color '#071311') 10 40
Fill-Ellipse $g @(199, 159, 52, 60) ([System.Drawing.Color]::FromArgb(235, 255, 255, 255))
Fill-Ellipse $g @(257, 283, 29, 31) ([System.Drawing.Color]::FromArgb(170, 255, 255, 255))
# Снежинка: шесть лучей с перекладинами — знак заморозки василиска.
$flakeX = 372; $flakeY = 352; $arm = 106; $ice = Get-Color '#d7f7ef'
$bars = @(@(0.52, 0.20), @(0.82, 0.11))
for ($i = 0; $i -lt 6; $i++) {
$angle = 15 + $i * 60
$dir = Get-CirclePoint 0 0 1 $angle
$side = Get-CirclePoint 0 0 1 ($angle + 90)
Draw-Line $g @($flakeX, $flakeY) (Get-CirclePoint $flakeX $flakeY $arm $angle) $ice 10 3.5
foreach ($bar in $bars) {
$px = $flakeX + $dir[0] * $arm * $bar[0]
$py = $flakeY + $dir[1] * $arm * $bar[0]
$half = $arm * $bar[1]
Draw-Line $g @(($px - $side[0] * $half), ($py - $side[1] * $half)) @(($px + $side[0] * $half), ($py + $side[1] * $half)) $ice 7 2.5
}
}
Draw-Ellipse $g @(($flakeX - 18), ($flakeY - 18), 36, 36) (Get-Color '#e1fffb') (Get-Color '#7fe3d5') 5 45
Save-Icon $canvas 'Game/assets/perks/basilisk_gaze.png'
}

# Иконки кнопок меню: профиль (голова лягушки), магазин скинов (домик), улучшения (стрелка вверх).
function Draw-ProfileIcon {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Ellipse $g @(64, 168, 384, 300) (Get-Color '#55a82e') (Get-Color '#357f22') 26
foreach ($ex in @(168, 344)) {
Draw-Ellipse $g @(($ex - 88), 104, 176, 176) (Get-Color '#66b83a') (Get-Color '#3d8a24') 24
Fill-Ellipse $g @(($ex - 52), 140, 104, 104) (Get-Color '#f6ffe9')
Fill-Ellipse $g @(($ex - 26), 166, 52, 52) (Get-Color '#173b12')
}
$mouth = New-Object System.Drawing.Drawing2D.GraphicsPath
$mouth.AddBezier(150, 356, 210, 412, 302, 412, 362, 356)
$pen = New-Pen (Get-Color '#173b12') 12
$g.DrawPath($pen, $mouth)
$pen.Dispose(); $mouth.Dispose()
Save-Icon $canvas 'Game/assets/ui/icon_profile.png'
}

function Draw-ShopIcon {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Path $g (New-Polygon @(@(52, 262), @(460, 262), @(256, 78))) (Get-Color '#e0554b') (Get-Color '#c9403a') 26
Draw-Path $g (New-Polygon @(@(114, 262), @(398, 262), @(398, 452), @(114, 452))) (Get-Color '#f7e6c4') (Get-Color '#e2c894') 26
Draw-Path $g (New-Polygon @(@(214, 322), @(298, 322), @(298, 452), @(214, 452))) (Get-Color '#a06a3c') (Get-Color '#7d5029') 16
Fill-Ellipse $g @(272, 382, 22, 22) (Get-Color '#ffd45e')
Draw-Ellipse $g @(140, 292, 78, 78) (Get-Color '#eaf6ff') (Get-Color '#9fd3f0') 14
Save-Icon $canvas 'Game/assets/ui/icon_shop.png'
}

function Draw-UpIcon {
$canvas = New-Canvas; $g = $canvas[1]
$arrow = New-Polygon @(@(256, 88), @(356, 216), @(302, 216), @(302, 448), @(210, 448), @(210, 216), @(156, 216))
Draw-Path $g $arrow (Get-Color '#8ce0b6') (Get-Color '#3fae8a') 26
Save-Icon $canvas 'Game/assets/ui/icon_upgrade.png'
}

Draw-HealthIcons
Draw-StickyPaws
Draw-RubberGrip
Draw-RapidFire
Draw-SlowZone
Draw-GoldRush
Draw-FruitExplosion
Draw-ComboMaster
Draw-Shell
Draw-TrashBounce
Draw-SecondLife
Draw-LightningTongue
Draw-BasiliskGaze
Draw-LilyPad
Draw-LilyFlower
Draw-ProfileIcon
Draw-ShopIcon
Draw-UpIcon


Write-Host '19 icons generated.'
