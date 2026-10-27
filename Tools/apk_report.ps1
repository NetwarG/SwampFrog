<#
.SYNOPSIS
	Разбор состава собранного APK по группам файлов.

.DESCRIPTION
	Печатает, сколько места в APK занимают нативные библиотеки каждой архитектуры,
	статические архивы Mono, C#-сборки, ресурсы игры и ресурсы Android.
	Файл подключается к Tools\build_android.ps1; можно вызвать и вручную:
	. Tools\apk_report.ps1; Show-ApkSizeReport -ApkPath build\SwampFrog.apk

.EXAMPLE
	Show-ApkSizeReport -ApkPath build\SwampFrog.apk
#>

# Возвращает группы файлов APK: по строке на архитектуру и на остальные части сборки.
function Get-ApkReportGroups([object[]]$Entries) {
	$groups = @()
	$abis = @($Entries | Where-Object { $_.FullName -match '^lib/([^/]+)/' } | ForEach-Object { $Matches[1] } | Sort-Object -Unique)
	foreach ($abi in $abis) {
		$prefix = "^lib/$([regex]::Escape($abi))/"
		$groups += @{ Title = "lib/$abi/*.so (нативные библиотеки)"; Pattern = "$prefix.*\.so$" }
		$groups += @{ Title = "lib/$abi/*.a (статические архивы Mono)"; Pattern = "$prefix.*\.a$" }
	}
	$groups += @{ Title = "assets/.godot/mono/publish (C#-сборки)"; Pattern = '^assets/\.godot/mono/publish/' }
	$groups += @{ Title = "assets/ (pck и ресурсы игры)"; Pattern = '^assets/(?!\.godot/mono/)' }
	$groups += @{ Title = "classes*.dex, res/, манифест"; Pattern = '^(classes\d*\.dex|res/|resources\.arsc|AndroidManifest\.xml)' }
	return $groups
}

# Печатает строку отчёта: название группы, сжатый размер в APK и число файлов.
function Write-ApkReportLine([string]$Title, [object[]]$Entries) {
	$size = [math]::Round((($Entries | Measure-Object CompressedLength -Sum).Sum) / 1MB, 2)
	Write-Host ("    {0,-52} {1,8} МБ  {2} файлов" -f $Title, $size, $Entries.Count)
}

# Показывает состав APK и предупреждает, если в него снова попали статические архивы Mono.
function Show-ApkSizeReport([string]$ApkPath) {
	if (!(Test-Path -LiteralPath $ApkPath)) { return }
	Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
	$zip = [System.IO.Compression.ZipFile]::OpenRead($ApkPath)
	try {
		$entries = @($zip.Entries)
		Write-Host "==> Состав APK (сжатый размер)" -ForegroundColor Cyan
		$used = @()
		foreach ($group in (Get-ApkReportGroups -Entries $entries)) {
			$matched = @($entries | Where-Object { $_.FullName -match $group.Pattern })
			$used += $matched
			Write-ApkReportLine -Title $group.Title -Entries $matched
		}
		Write-ApkReportLine -Title "прочее" -Entries @($entries | Where-Object { $used -notcontains $_ })
		Write-ApkReportLine -Title "всего" -Entries $entries

		$archives = @($entries | Where-Object { $_.FullName -match '^lib/[^/]+/.*\.a$' })
		if ($archives.Count) {
			$size = [math]::Round((($archives | Measure-Object CompressedLength -Sum).Sum) / 1MB, 2)
			Write-Host "!!  В APK есть статические архивы Mono (*.a, $size МБ): их исключает цель StripMonoStaticLibraries в SwampFrog.csproj." -ForegroundColor Yellow
		}
	}
	finally {
		$zip.Dispose()
	}
}
