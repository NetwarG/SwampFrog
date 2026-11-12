Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

# Иконки достижений (PNG 512x512) в том же стикер-стиле, что generate_icons.ps1:
# толстый коричневый контур, диагональный градиент заливки. Имя файла совпадает
# с полем IconName в Game/Meta/AchievementCatalog.cs.

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

# Первый забег: голова лягушки.
function Draw-AchFirstRun {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Ellipse $g @(88, 176, 336, 260) (Get-Color '#55a82e') (Get-Color '#357f22') 24
foreach ($ex in @(184, 328)) {
Draw-Ellipse $g @(($ex - 76), 128, 152, 152) (Get-Color '#66b83a') (Get-Color '#3d8a24') 22
Fill-Ellipse $g @(($ex - 44), 160, 88, 88) (Get-Color '#f6ffe9')
Fill-Ellipse $g @(($ex - 22), 182, 44, 44) (Get-Color '#173b12')
}
$mouth = New-Object System.Drawing.Drawing2D.GraphicsPath
$mouth.AddBezier(168, 344, 214, 396, 298, 396, 344, 344)
$pen = New-Pen (Get-Color '#173b12') 12
$g.DrawPath($pen, $mouth); $pen.Dispose(); $mouth.Dispose()
Save-Icon $canvas 'Game/assets/achievements/ach_first_run.png'
}

# Ветеран: медаль на лентах со звездой.
function Draw-AchVeteran {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Path $g (New-Polygon @(@(206, 84), @(246, 84), @(228, 300), @(196, 300))) (Get-Color '#8fd9f5') (Get-Color '#3a94c8') 20
Draw-Path $g (New-Polygon @(@(266, 84), @(306, 84), @(316, 300), @(284, 300))) (Get-Color '#c6f089') (Get-Color '#6fae2f') 20
Draw-Ellipse $g @(156, 244, 200, 200) (Get-Color '#ffe066') (Get-Color '#e39b18') 22
Draw-Path $g (New-Star 256 344 82 34 5) (Get-Color '#fff6c8') (Get-Color '#f2c33d') 12 45
Save-Icon $canvas 'Game/assets/achievements/ach_veteran.png'
}

# Садовник: яблоко.
function Draw-AchFruits {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Apple $g 256 288 300 (Get-Color '#ff6b5a') (Get-Color '#d63b2f') 24
Save-Icon $canvas 'Game/assets/achievements/ach_fruits.png'
}

# Фруктовый сад: яблоко со звездой.
function Draw-AchFruitsBig {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Apple $g 232 304 276 (Get-Color '#ff6b5a') (Get-Color '#d63b2f') 24
Draw-Path $g (New-Star 388 152 66 28 5) (Get-Color '#ffe066') (Get-Color '#e39b18') 12 45
Save-Icon $canvas 'Game/assets/achievements/ach_fruits_big.png'
}

# Золотая жила: золотое яблоко.
function Draw-AchGolden {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Apple $g 256 288 300 (Get-Color '#ffe066') (Get-Color '#e39b18') 24
Save-Icon $canvas 'Game/assets/achievements/ach_golden.png'
}

# Золотой мастер: золотое яблоко под короной.
function Draw-AchGoldenBig {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Apple $g 256 320 264 (Get-Color '#ffe066') (Get-Color '#e39b18') 24
Draw-Path $g (New-Polygon @(@(150, 172), @(166, 66), @(214, 132), @(256, 52), @(298, 132), @(346, 66), @(362, 172))) (Get-Color '#fff3b0') (Get-Color '#f2c33d') 22
Draw-Path $g (New-Polygon @(@(150, 172), @(362, 172), @(362, 214), @(150, 214))) (Get-Color '#ffe066') (Get-Color '#d9a324') 20
Save-Icon $canvas 'Game/assets/achievements/ach_golden_big.png'
}


# Первый рубеж: одна бронзовая звезда.
function Draw-AchScoreFirst {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Path $g (New-Star 256 256 176 74 5) (Get-Color '#f0b27a') (Get-Color '#b06a2c') 24 45
Save-Icon $canvas 'Game/assets/achievements/ach_score1.png'
}

# Полсотни: две серебряные звезды.
function Draw-AchScoreFifty {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Path $g (New-Star 198 300 132 56 5) (Get-Color '#eef3f7') (Get-Color '#a7b6c2') 22 45
Draw-Path $g (New-Star 348 208 108 46 5) (Get-Color '#eef3f7') (Get-Color '#a7b6c2') 20 45
Save-Icon $canvas 'Game/assets/achievements/ach_score2.png'
}

# Сотня: золотая корона.
function Draw-AchScoreHundred {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Path $g (New-Polygon @(@(132, 336), @(146, 172), @(202, 250), @(256, 148), @(310, 250), @(366, 172), @(380, 336))) (Get-Color '#fff3b0') (Get-Color '#f2c33d') 24
Draw-Path $g (New-Polygon @(@(126, 330), @(386, 330), @(386, 392), @(126, 392))) (Get-Color '#ffe066') (Get-Color '#d9a324') 22
Fill-Ellipse $g @(176, 200, 40, 40) ([System.Drawing.Color]::FromArgb(220, 255, 255, 255))
Save-Icon $canvas 'Game/assets/achievements/ach_score3.png'
}

# Ученик слияний: два круга сливаются (плюс).
function Draw-AchMerge {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Ellipse $g @(84, 244, 180, 180) (Get-Color '#ff6b5a') (Get-Color '#d63b2f') 22
Draw-Ellipse $g @(248, 244, 180, 180) (Get-Color '#8ad552') (Get-Color '#3d861d') 22
Draw-Line $g @(256, 92) @(256, 190) (Get-Color '#ffe066') 26 10
Draw-Line $g @(207, 141) @(305, 141) (Get-Color '#ffe066') 26 10
Save-Icon $canvas 'Game/assets/achievements/ach_merge.png'
}

# Мастер слияний: пирамида кругов.
function Draw-AchMergeBig {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Ellipse $g @(112, 300, 148, 148) (Get-Color '#ff6b5a') (Get-Color '#d63b2f') 22
Draw-Ellipse $g @(252, 300, 148, 148) (Get-Color '#8ad552') (Get-Color '#3d861d') 22
Draw-Ellipse $g @(182, 138, 180, 180) (Get-Color '#ffe066') (Get-Color '#e39b18') 26
Save-Icon $canvas 'Game/assets/achievements/ach_merge_big.png'
}

# Suika-чемпион: кувшин с фруктами.
function Draw-AchSuika {
$canvas = New-Canvas; $g = $canvas[1]
$jar = New-Polygon @(@(160, 150), @(352, 150), @(388, 424), @(124, 424))
Draw-Path $g $jar (Get-Color '#d9f6ff') (Get-Color '#9fd8ef') 26
Draw-Ellipse $g @(136, 118, 240, 62) (Get-Color '#eafaff') (Get-Color '#9fd8ef') 20
Fill-Ellipse $g @(196, 320, 74, 74) (Get-Color '#8ad552')
Fill-Ellipse $g @(262, 300, 62, 62) (Get-Color '#ff6b5a')
Fill-Ellipse $g @(212, 250, 54, 54) (Get-Color '#ffd45e')
Save-Icon $canvas 'Game/assets/achievements/ach_suika.png'
}

# Модник: шляпа-цилиндр.
function Draw-AchFashion {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Path $g (New-Polygon @(@(172, 120), @(340, 120), @(356, 344), @(156, 344))) (Get-Color '#7a68b0') (Get-Color '#3a2f5c') 24
Draw-Ellipse $g @(96, 330, 320, 68) (Get-Color '#6a589e') (Get-Color '#2f2749') 22
Draw-Path $g (New-Polygon @(@(160, 300), @(352, 300), @(352, 344), @(160, 344))) (Get-Color '#ffe066') (Get-Color '#e39b18') 16
Save-Icon $canvas 'Game/assets/achievements/ach_fashion.png'
}

# Прокачка: стрелка вверх.
function Draw-AchUpgrader {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Path $g (New-Polygon @(@(256, 84), @(392, 236), @(304, 236), @(304, 420), @(208, 420), @(208, 236), @(120, 236))) (Get-Color '#8ad552') (Get-Color '#3d861d') 26
Save-Icon $canvas 'Game/assets/achievements/ach_upgrader.png'
}

Draw-AchFirstRun
Draw-AchVeteran
Draw-AchFruits
Draw-AchFruitsBig
Draw-AchGolden
Draw-AchGoldenBig
Draw-AchScoreFirst
Draw-AchScoreFifty
Draw-AchScoreHundred
Draw-AchMerge
Draw-AchMergeBig
Draw-AchSuika
Draw-AchFashion
Draw-AchUpgrader

