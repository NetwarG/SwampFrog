Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
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
$path = Join-Path $root $relativePath
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

function Draw-ArcArrow($g, $rect, $start, $sweep, $color, $width, $head) {
$pen = New-Pen $outline ($width + 18)
$g.DrawArc($pen, $rect[0], $rect[1], $rect[2], $rect[3], $start, $sweep)
$pen.Dispose()
$pen = New-Pen $color $width
$g.DrawArc($pen, $rect[0], $rect[1], $rect[2], $rect[3], $start, $sweep)
$pen.Dispose()
Draw-Path $g (New-Polygon $head) $color $color 14
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
Draw-Line $g @(185, 265) @(151, 100) $green 58 15
Draw-Line $g @(238, 244) @(234, 68) $green 58 15
Draw-Line $g @(290, 255) @(319, 104) $green 58 15
Draw-Line $g @(170, 300) @(82, 245) $green 62 15
Draw-Ellipse $g @(108, 166, 260, 258) $light $green 24 50
Draw-Apple $g 377 173 150 (Get-Color '#ff5548') (Get-Color '#cf2438') 18
Fill-Ellipse $g @(159, 213, 42, 58) ([System.Drawing.Color]::FromArgb(135, 255, 255, 255))
Save-Icon $canvas 'Game/assets/perks/sticky_paws.png'
}

function Draw-RubberGrip {
$canvas = New-Canvas; $g = $canvas[1]
Draw-ArcArrow $g @(60, 62, 392, 382) 135 190 (Get-Color '#ffd45e') 42 @(@(390, 73), @(451, 144), @(365, 153))
Draw-Ellipse $g @(121, 160, 248, 244) (Get-Color '#b4e863') (Get-Color '#62aa2e') 24 45
Draw-Line $g @(159, 240) @(315, 240) (Get-Color '#4d9128') 15 0
Draw-Line $g @(172, 295) @(304, 295) (Get-Color '#4d9128') 15 0
Fill-Ellipse $g @(162, 189, 45, 58) ([System.Drawing.Color]::FromArgb(135, 255, 255, 255))
Save-Icon $canvas 'Game/assets/perks/rubber_grip.png'
}

function Draw-RapidFire {
$canvas = New-Canvas; $g = $canvas[1]; $green = Get-Color '#91d844'
Draw-Line $g @(48, 145) @(180, 145) (Get-Color '#ff8c7a') 28 9
Draw-Line $g @(35, 235) @(159, 235) (Get-Color '#ff8c7a') 28 9
Draw-Line $g @(62, 325) @(189, 325) (Get-Color '#ff8c7a') 28 9
Draw-Line $g @(137, 348) @(344, 226) $green 86 18
Draw-Ellipse $g @(288, 154, 139, 139) (Get-Color '#b4e863') $green 22 45
Draw-Line $g @(357, 207) @(447, 137) $green 48 14
Fill-Ellipse $g @(315, 178, 35, 43) ([System.Drawing.Color]::FromArgb(140, 255, 255, 255))
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
Draw-Line $g @(92, 404) @(403, 94) (Get-Color '#ffd45e') 31 11
Draw-Path $g (New-Polygon @(@(348, 64), @(455, 48), @(438, 154))) (Get-Color '#ffe76a') (Get-Color '#f0a62b') 16 45
Draw-Ellipse $g @(67, 288, 137, 137) (Get-Color '#ff6a62') (Get-Color '#c92c48') 22 40
Draw-Ellipse $g @(188, 192, 145, 145) (Get-Color '#d3b3ff') (Get-Color '#7c43ba') 22 40
Draw-Ellipse $g @(315, 91, 133, 133) (Get-Color '#ff6a62') (Get-Color '#c92c48') 22 40
Fill-Ellipse $g @(100, 316, 30, 38) ([System.Drawing.Color]::FromArgb(220, 255, 255, 255))
Fill-Ellipse $g @(221, 220, 32, 40) ([System.Drawing.Color]::FromArgb(220, 255, 255, 255))
Fill-Ellipse $g @(345, 118, 29, 36) ([System.Drawing.Color]::FromArgb(220, 255, 255, 255))
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
Draw-ArcArrow $g @(55, 48, 402, 396) 145 185 (Get-Color '#7fe3d5') 38 @(@(394, 71), @(459, 139), @(374, 151))
Draw-Path $g (New-Polygon @(@(142, 159), @(370, 159), @(338, 419), @(174, 419))) (Get-Color '#b8c8c5') (Get-Color '#65727f') 24 35
Draw-Line $g @(110, 155) @(402, 155) (Get-Color '#9aa9ad') 54 13
Draw-Line $g @(208, 101) @(304, 101) (Get-Color '#9aa9ad') 35 12
Draw-Line $g @(211, 225) @(222, 353) (Get-Color '#4e5b65') 18 0
Draw-Line $g @(256, 225) @(256, 353) (Get-Color '#4e5b65') 18 0
Draw-Line $g @(301, 225) @(290, 353) (Get-Color '#4e5b65') 18 0
Draw-Line $g @(159, 182) @(352, 182) (Get-Color '#d9e2df') 13 0
Save-Icon $canvas 'Game/assets/perks/trash_bounce.png'
}

function Draw-SecondLife {
$canvas = New-Canvas; $g = $canvas[1]
Draw-ArcArrow $g @(59, 60, 394, 394) 38 282 (Get-Color '#b4e863') 43 @(@(392, 45), @(462, 120), @(374, 133))
Draw-Heart $g 256 283 304 $false
Save-Icon $canvas 'Game/assets/perks/second_life.png'
}

function Draw-LightningTongue {
$canvas = New-Canvas; $g = $canvas[1]
Draw-Line $g @(82, 394) @(180, 318) (Get-Color '#ef6487') 72 17
$bolt = @(@(292, 35), @(111, 287), @(233, 270), @(183, 474), @(414, 188), @(286, 207))
Draw-Path $g (New-Polygon $bolt) (Get-Color '#ffb2c2') (Get-Color '#ef4677') 25 35
Draw-Line $g @(275, 102) @(203, 208) (Get-Color '#ffe5ed') 24 0
Fill-Ellipse $g @(63, 362, 38, 38) (Get-Color '#ff8faa')
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
for ($i = 0; $i -lt 6; $i++) {
$angle = $i * [Math]::PI / 3
$from = @((391 + [Math]::Cos($angle) * 19), (372 + [Math]::Sin($angle) * 19))
$to = @((391 + [Math]::Cos($angle) * 78), (372 + [Math]::Sin($angle) * 78))
Draw-Line $g $from $to (Get-Color '#b8fff5') 16 7
}
Draw-Ellipse $g @(374, 355, 34, 34) (Get-Color '#e1fffb') (Get-Color '#7fe3d5') 7 45
Save-Icon $canvas 'Game/assets/perks/basilisk_gaze.png'
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

Write-Host '14 icons generated.'
