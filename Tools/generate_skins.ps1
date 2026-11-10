Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

# Генератор иконок скинов лягушки (шляпа, перчатки, подставка).
# Рисует PNG 512x512 в Game/assets/skins/{id}.png в стиле остальных иконок:
# толстый коричневый контур, диагональный градиент заливки. Иконки легко заменить арт-файлом.

$projectRoot = Split-Path -Parent $PSScriptRoot
$outline = [System.Drawing.ColorTranslator]::FromHtml('#2b170f')

function Get-Color($hex) {
	return [System.Drawing.ColorTranslator]::FromHtml($hex)
}

function New-Canvas {
	$bitmap = New-Object System.Drawing.Bitmap 512, 512
	$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
	$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
	$graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
	$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
	$graphics.Clear([System.Drawing.Color]::Transparent)
	return @($bitmap, $graphics)
}

function Save-Icon($canvas, $name) {
	$directory = Join-Path $projectRoot 'Game/assets/skins'
	[void](New-Item -ItemType Directory -Path $directory -Force)
	$path = Join-Path $directory "$name.png"
	$canvas[0].Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
	$canvas[1].Dispose()
	$canvas[0].Dispose()
	Write-Host "skin icon: $name.png"
}

function New-Pen($color, $width) {
	$pen = New-Object System.Drawing.Pen $color, $width
	$pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
	$pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
	$pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
	return $pen
}

function Draw-Line($g, $from, $to, $color, $width) {
	$pen = New-Pen $color $width
	$g.DrawLine($pen, $from[0], $from[1], $to[0], $to[1])
	$pen.Dispose()
}

function Draw-Path($g, $path, $colorA, $colorB = $null, $border = 20, $angle = 45) {
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

function Draw-Ellipse($g, $rect, $colorA, $colorB = $null, $border = 20) {
	$path = New-Object System.Drawing.Drawing2D.GraphicsPath
	$path.AddEllipse($rect[0], $rect[1], $rect[2], $rect[3])
	Draw-Path $g $path $colorA $colorB $border
}

function Fill-Ellipse($g, $rect, $color, $border = 0) {
	$path = New-Object System.Drawing.Drawing2D.GraphicsPath
	$path.AddEllipse($rect[0], $rect[1], $rect[2], $rect[3])
	if ($border -gt 0) {
		Draw-Path $g $path $color $null $border
		return
	}
	$brush = New-Object System.Drawing.SolidBrush $color
	$g.FillPath($brush, $path)
	$brush.Dispose()
	$path.Dispose()
}

function Draw-Rect($g, $rect, $colorA, $colorB = $null, $border = 20) {
	if ($null -eq $colorB) { $colorB = $colorA }
	$r = New-Object System.Drawing.Rectangle $rect[0], $rect[1], $rect[2], $rect[3]
	$path = New-Object System.Drawing.Drawing2D.GraphicsPath
	$path.AddRectangle($r)
	Draw-Path $g $path $colorA $colorB $border 90
}

function New-Polygon($points) {
	$list = New-Object 'System.Collections.Generic.List[System.Drawing.PointF]'
	foreach ($point in $points) {
		$list.Add([System.Drawing.PointF]::new([float]$point[0], [float]$point[1]))
	}
	$path = New-Object System.Drawing.Drawing2D.GraphicsPath
	$path.AddPolygon($list.ToArray())
	return $path
}

function New-RoundedRect($x, $y, $w, $h, $r) {
	$d = $r * 2
	$path = New-Object System.Drawing.Drawing2D.GraphicsPath
	$path.AddArc($x, $y, $d, $d, 180, 90)
	$path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
	$path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
	$path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
	$path.CloseFigure()
	return $path
}

# ---------- Шляпы ----------

function New-PartyHat {
	$c = New-Canvas
	$g = $c[1]
	$cone = New-Polygon @(@(126, 442), @(386, 442), @(256, 92))
	Draw-Path $g $cone (Get-Color '#e0554b') (Get-Color '#c9403a') 22 90
	Draw-Line $g @(168, 320) @(344, 320) (Get-Color '#ffd45e') 24
	Draw-Line $g @(196, 218) @(316, 218) (Get-Color '#ffd45e') 24
	Fill-Ellipse $g @(206, 42, 100, 100) (Get-Color '#ffd45e') 18
	Save-Icon $c 'partyhat'
}

function New-TopHat {
	$c = New-Canvas
	$g = $c[1]
	Fill-Ellipse $g @(96, 358, 320, 92) (Get-Color '#1b1a24') $null 20
	Draw-Rect $g @(151, 150, 210, 236) (Get-Color '#2b2a38') (Get-Color '#1b1a24') 18
	Draw-Rect $g @(156, 316, 200, 58) (Get-Color '#8b3a3a') $null 0
	Save-Icon $c 'tophat'
}

function New-FlowerHat {
	$c = New-Canvas
	$g = $c[1]
	for ($i = 0; $i -lt 5; $i++) {
		$angle = [Math]::PI * 2 * $i / 5
		$x = 256 + [Math]::Cos($angle) * 96 - 88
		$y = 256 + [Math]::Sin($angle) * 96 - 88
		Draw-Ellipse $g @($x, $y, 176, 176) (Get-Color '#ff9ec0') (Get-Color '#ff8fb1') 20
	}
	Fill-Ellipse $g @(188, 188, 136, 136) (Get-Color '#ffd45e') 18
	Save-Icon $c 'flowerhat'
}

function New-MushroomHat {
	$c = New-Canvas
	$g = $c[1]
	Draw-Rect $g @(212, 322, 88, 130) (Get-Color '#e8d6b8') (Get-Color '#d8c4a0') 18
	$dome = New-Object System.Drawing.Drawing2D.GraphicsPath
	$dome.AddArc(76, 160, 360, 360, 180, 180)
	$dome.CloseFigure()
	Draw-Path $g $dome (Get-Color '#d94848') (Get-Color '#b83a3a') 22 90
	Fill-Ellipse $g @(164, 244, 52, 52) (Get-Color '#ffe9cf')
	Fill-Ellipse $g @(274, 229, 52, 52) (Get-Color '#ffe9cf')
	Fill-Ellipse $g @(224, 179, 52, 52) (Get-Color '#ffe9cf')
	Save-Icon $c 'mushroomhat'
}

# ---------- Перчатки ----------

function New-Glove($name, $lightHex, $darkHex, $cuffHex, $striped) {
	$c = New-Canvas
	$g = $c[1]
	$light = Get-Color $lightHex
	$dark = Get-Color $darkHex
	$cuff = Get-Color $cuffHex
	# Манжета — вертикальная полоска слева.
	Draw-Path $g (New-RoundedRect 64 118 118 276 36) $cuff $cuff 22
	Draw-Line $g @(150, 150) @(150, 362) $dark 14
	# Кулак.
	Draw-Path $g (New-RoundedRect 168 92 268 328 110) $light $dark 24
	if ($striped) {
		Draw-Line $g @(238, 150) @(238, 362) (Get-Color '#ffffff') 26
		Draw-Line $g @(312, 150) @(312, 362) (Get-Color '#ffffff') 26
	}
	# Пальцы — кружочки на внешней стороне кулака.
	foreach ($finger in @(@(416, 150), @(446, 254), @(416, 358))) {
		Draw-Ellipse $g @(($finger[0] - 54), ($finger[1] - 54), 108, 108) $light $dark 18
	}
	Save-Icon $c $name
}

# ---------- Подставки ----------

function New-LilyPadStand {
	$c = New-Canvas
	$g = $c[1]
	$source = [System.Drawing.Image]::FromFile((Join-Path $projectRoot 'Game/assets/ui/lily_pad.png'))
	$size = 340
	$g.DrawImage($source, 256 - $size / 2, 256 - $size / 2, $size, $size)
	$source.Dispose()
	Save-Icon $c 'lilypadstand'
}

function New-CarpetStand {
	$c = New-Canvas
	$g = $c[1]
	Draw-Ellipse $g @(36, 167, 440, 176) (Get-Color '#a83b47') (Get-Color '#8f2f3a') 22
	Fill-Ellipse $g @(76, 197, 360, 116) (Get-Color '#d15a63')
	Fill-Ellipse $g @(176, 225, 160, 60) (Get-Color '#8f2f3a')
	Save-Icon $c 'carpetstand'
}

function New-StoneStand {
	$c = New-Canvas
	$g = $c[1]
	Draw-Ellipse $g @(56, 177, 400, 160) (Get-Color '#8b9099') (Get-Color '#5f636b') 22
	Fill-Ellipse $g @(106, 205, 300, 104) (Get-Color '#9aa0a8')
	Save-Icon $c 'stonestand'
}

function New-StumpStand {
	$c = New-Canvas
	$g = $c[1]
	Draw-Ellipse $g @(66, 171, 380, 170) (Get-Color '#8a6136') (Get-Color '#6b4a2f') 22
	Fill-Ellipse $g @(106, 197, 300, 118) (Get-Color '#a87a4f')
	Fill-Ellipse $g @(166, 225, 180, 62) (Get-Color '#8a6136')
	Fill-Ellipse $g @(216, 243, 80, 26) (Get-Color '#6b4a2f')
	Save-Icon $c 'stumpstand'
}

# ---------- Запуск ----------

Write-Host 'Generating skin icons...'
New-PartyHat
New-TopHat
New-FlowerHat
New-MushroomHat
New-Glove 'boxinggloves' '#e0554b' '#c9403a' '#ffd45e' $false
New-Glove 'mintgloves' '#7fe0c0' '#4fbf9f' '#d8fff2' $false
New-Glove 'captaingloves' '#4f7fd6' '#3a63b5' '#f2f4ff' $false
New-Glove 'stripedgloves' '#8f7fd6' '#715fc0' '#ffffff' $true
New-LilyPadStand
New-CarpetStand
New-StoneStand
New-StumpStand
Write-Host 'Skin icons done.'


