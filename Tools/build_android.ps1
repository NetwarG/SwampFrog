<#
.SYNOPSIS
	Сборка Android-версии SwampFrog по пресету из export_presets.cfg и установка её
	на подключённый эмулятор или телефон.

.EXAMPLE
	Tools\build_android.ps1
	Собрать debug-APK пресетом 'Android', установить и запустить на подключённом устройстве.

.EXAMPLE
	Tools\build_android.ps1 -AvdName Pixel_9 -Logs
	Поднять эмулятор, если устройств нет, и показать лог игры после запуска.

.EXAMPLE
	Tools\build_android.ps1 -Preset "Android Emulator" -AvdName Pixel_10
	Собрать x86_64-APK пресетом для эмулятора и запустить его на эмуляторе.

.EXAMPLE
	Tools\build_android.ps1 -DryRun
	Только проверить окружение, пресет и устройство, ничего не собирая.

.EXAMPLE
	Tools\build_android.ps1 -NoBuild
	Установить и запустить уже собранный build\SwampFrog.apk, не пересобирая его.

.EXAMPLE
	Tools\build_android.ps1 -BuildTimeout 300
	Ограничить сборку пятью минутами: дольше процесс будет снят принудительно.

.EXAMPLE
	Tools\build_android.ps1 -Mode release
	Собрать release-APK: ключ для подписи скрипт найдёт сам, а при отсутствии создаст.
#>
[CmdletBinding()]
param(
	[string]$Preset = "Android",
	[ValidateSet("debug", "release")][string]$Mode = "debug",
	[string]$AvdName = "",
	[switch]$FixArch,
	[switch]$NoInstall,
	[switch]$NoLaunch,
	[switch]$NoBuild,
	[switch]$Logs,
	[switch]$DryRun,
	[switch]$ConsoleWrapper,
	[int]$BuildTimeout = 900,
	[string]$Godot = "",
	[string]$Adb = "",
	[string]$ReleaseKeystore = "",
	[string]$ReleaseUser = "",
	[string]$ReleasePassword = ""
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$Text) { Write-Host "==> $Text" -ForegroundColor Cyan }
function Write-Info([string]$Text) { Write-Host "    $Text" }
function Write-Notice([string]$Text) { Write-Host "!!  $Text" -ForegroundColor Yellow }
function Fail([string]$Text) { Write-Host "ОШИБКА: $Text" -ForegroundColor Red; exit 1 }

# Запускает внешнюю программу и возвращает её вывод и код возврата.
# Внешние программы пишут в stderr, поэтому на время вызова снимаем строгий режим ошибок.
function Invoke-Native([string]$FilePath, [string[]]$Arguments) {
	$previous = $ErrorActionPreference
	$ErrorActionPreference = "Continue"
	try {
		$output = & $FilePath @Arguments 2>&1 | Out-String
		return [pscustomobject]@{ Output = $output; Code = $LASTEXITCODE }
	}
	finally {
		$ErrorActionPreference = $previous
	}
}

# Сколько секунд APK должен не меняться, чтобы считать процесс сборки зависшим.
$script:QuietSeconds = 30

# Управляющие последовательности ANSI в выводе Godot (цветной прогресс) - вырезаем их.
$script:AnsiPattern = [char]27 + '\[[0-9;]*[A-Za-z]'

# Возвращает новые строки лога и число уже прочитанных строк.
function Get-LogTail([string]$Path, [int]$Shown, [switch]$Flush) {
	$result = [pscustomobject]@{ Text = ""; Shown = $Shown }
	if (!(Test-Path -LiteralPath $Path)) { return $result }
	$text = ""
	try {
		# Файл держит процесс сборки, поэтому открываем его с общим доступом на запись.
		$stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
		try {
			$reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::UTF8)
			$text = $reader.ReadToEnd()
		}
		finally { $stream.Dispose() }
	}
	catch { return $result }
	# Godot обновляет прогресс через возврат каретки, поэтому строками считаем и "\r".
	$lines = $text -split "`r`n|`n|`r"
	$complete = $text.EndsWith("`n") -or $text.EndsWith("`r")
	if ($complete) {
		# Текст заканчивается переводом строки: последний пустой элемент строкой не является.
		$lines = if ($lines.Count -gt 1) { $lines[0..($lines.Count - 2)] } else { @() }
	}
	elseif (!$Flush) {
		# Последняя строка может быть ещё не дописана - покажем её в следующий раз.
		$lines = if ($lines.Count -gt 1) { $lines[0..($lines.Count - 2)] } else { @() }
	}
	if ($lines.Count -le $Shown) { return $result }
	$result.Text = ($lines[$Shown..($lines.Count - 1)] -join "`n")
	$result.Shown = $lines.Count
	return $result
}

# Печатает новые строки лога сборки и складывает их в общий вывод.
function Write-BuildTail([string]$Path, [int]$Shown, [System.Collections.Generic.List[string]]$Sink, [switch]$Flush) {
	$tail = Get-LogTail -Path $Path -Shown $Shown -Flush:$Flush
	foreach ($line in ($tail.Text -split "`n")) {
		$clean = ($line -replace $script:AnsiPattern, '').TrimEnd()
		if (!$clean.Trim()) { continue }
		Write-Host "    | $clean" -ForegroundColor DarkGray
		$Sink.Add($clean)
	}
	return $tail.Shown
}

# Запускает внешнюю программу, печатает её вывод по мере появления и возвращает результат сборки.
# Если $WatchedFile перестал меняться, а процесс жив, дерево процессов снимается принудительно:
# так ведёт себя console-обёртка Godot, которая ждёт серверные процессы dotnet (godot#110101).
function Invoke-BuildStreaming([string]$FilePath, [string[]]$Arguments, [hashtable]$Environment, [string]$WatchedFile, [int]$TimeoutSec) {
	$logPath = Join-Path $env:TEMP "swampfrog_build_$PID.log"
	$errorLogPath = "$logPath.err"
	Remove-Item -LiteralPath $logPath, $errorLogPath -ErrorAction SilentlyContinue

	$savedEnv = @{}
	foreach ($key in $Environment.Keys) {
		$savedEnv[$key] = [Environment]::GetEnvironmentVariable($key)
		[Environment]::SetEnvironmentVariable($key, $Environment[$key])
	}

	$result = [pscustomobject]@{ Code = -1; Output = ""; TimedOut = $false; Hung = $false }
	$sink = New-Object System.Collections.Generic.List[string]
	try {
		$process = Start-Process -FilePath $FilePath -ArgumentList $Arguments -NoNewWindow `
			-RedirectStandardOutput $logPath -RedirectStandardError $errorLogPath -PassThru
		$startTime = Get-Date
		$deadline = $startTime.AddSeconds($TimeoutSec)
		$shownOut = 0
		$shownErr = 0
		$stableSince = $null
		$cpuAtStable = 0
		$lastStamp = ""
		while (!$process.HasExited) {
			$shownOut = Write-BuildTail -Path $logPath -Shown $shownOut -Sink $sink
			$shownErr = Write-BuildTail -Path $errorLogPath -Shown $shownErr -Sink $sink
			if ((Get-Date) -gt $deadline) {
				$result.TimedOut = $true
				break
			}
			try {
				if ($WatchedFile -and (Test-Path -LiteralPath $WatchedFile)) {
					$fileInfo = Get-Item -LiteralPath $WatchedFile
					$stamp = "$($fileInfo.Length):$($fileInfo.LastWriteTimeUtc.Ticks)"
					if ($fileInfo.LastWriteTime -lt $startTime) {
						# APK от прошлого прогона - ждём, пока Godot перезапишет его.
						$stableSince = $null
					}
					elseif ($stamp -ne $lastStamp) {
						$lastStamp = $stamp
						$stableSince = Get-Date
						$cpuAtStable = $process.TotalProcessorTime.TotalSeconds
					}
					elseif ($stableSince -and ((Get-Date) - $stableSince).TotalSeconds -ge $script:QuietSeconds) {
						# APK не меняется и процесс не грузит CPU - это зависание на завершении.
						if ($process.TotalProcessorTime.TotalSeconds - $cpuAtStable -lt 1) {
							$result.Hung = $true
							break
						}
						$stableSince = Get-Date
						$cpuAtStable = $process.TotalProcessorTime.TotalSeconds
					}
				}
			}
			catch { $stableSince = $null }
			Start-Sleep -Milliseconds 500
			$process.Refresh()
		}

		if ($process.HasExited) {
			# В Windows PowerShell 5.1 с перенаправлением вывода ExitCode пустой: код возврата не показатель.
			$result.Code = $process.ExitCode
		}
		else {
			Write-Notice "Процесс сборки не завершается - снимаю его дерево процессов."
			Invoke-Native "taskkill" @("/PID", "$($process.Id)", "/T", "/F") | Out-Null
			$process.WaitForExit(15000) | Out-Null
		}

		# Даём файлам лога дописаться, иначе последние строки Godot останутся невидимыми.
		Start-Sleep -Milliseconds 300
		$shownOut = Write-BuildTail -Path $logPath -Shown $shownOut -Sink $sink -Flush
		$shownErr = Write-BuildTail -Path $errorLogPath -Shown $shownErr -Sink $sink -Flush
		$result.Output = ($sink -join "`n")
	}
	finally {
		foreach ($key in $savedEnv.Keys) {
			if ($null -eq $savedEnv[$key]) { Remove-Item -Path "env:$key" -ErrorAction SilentlyContinue }
			else { [Environment]::SetEnvironmentVariable($key, $savedEnv[$key]) }
		}
	}
	return $result
}

# ---------- Окружение ----------

$root = Split-Path -Parent $PSScriptRoot
$presetsPath = Join-Path $root "export_presets.cfg"

function Find-Godot([string]$Explicit) {
	if ($Explicit) {
		if (Test-Path -LiteralPath $Explicit) { return (Resolve-Path -LiteralPath $Explicit).Path }
		Fail "Godot не найден по пути: $Explicit"
	}
	if ($env:GODOT4 -and (Test-Path -LiteralPath $env:GODOT4)) { return $env:GODOT4 }
	$command = Get-Command godot -ErrorAction SilentlyContinue
	if ($command) { return $command.Source }
	foreach ($dir in @("C:\Godot", (Join-Path $env:USERPROFILE "Godot"), (Join-Path $env:LOCALAPPDATA "Programs\Godot"))) {
		if (!(Test-Path -LiteralPath $dir)) { continue }
		# Основной exe не тянет потомков через Job Object - предпочитаем его:
		# console-обёртка ждёт серверные процессы dotnet и висит после готовности APK.
		if (!$script:UseConsoleWrapper) {
			$main = Get-ChildItem -LiteralPath $dir -Filter "Godot*.exe" -File -ErrorAction SilentlyContinue |
				Where-Object { $_.Name -notmatch 'console\.exe$' } | Sort-Object Name -Descending | Select-Object -First 1
			if ($main) { return $main.FullName }
		}
		$console = Get-ChildItem -LiteralPath $dir -Filter "Godot*console.exe" -File -ErrorAction SilentlyContinue |
			Sort-Object Name -Descending | Select-Object -First 1
		if ($console) { return $console.FullName }
		$any = Get-ChildItem -LiteralPath $dir -Filter "Godot*.exe" -File -ErrorAction SilentlyContinue |
			Sort-Object Name -Descending | Select-Object -First 1
		if ($any) { return $any.FullName }
	}
	Fail "Не найден исполняемый файл Godot. Укажите его параметром -Godot <путь>."
}

function Find-Adb([string]$Explicit) {
	if ($Explicit) {
		if (Test-Path -LiteralPath $Explicit) { return (Resolve-Path -LiteralPath $Explicit).Path }
		Fail "adb не найден по пути: $Explicit"
	}
	foreach ($name in @("ANDROID_HOME", "ANDROID_SDK_ROOT")) {
		$sdk = [Environment]::GetEnvironmentVariable($name)
		if ($sdk) {
			$candidate = Join-Path $sdk "platform-tools\adb.exe"
			if (Test-Path -LiteralPath $candidate) { return $candidate }
		}
	}
	$local = Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"
	if (Test-Path -LiteralPath $local) { return $local }
	$command = Get-Command adb -ErrorAction SilentlyContinue
	if ($command) { return $command.Source }
	Fail "Не найден adb. Установите platform-tools или укажите путь параметром -Adb <путь>."
}

function Find-Emulator {
	$local = Join-Path $env:LOCALAPPDATA "Android\Sdk\emulator\emulator.exe"
	if (Test-Path -LiteralPath $local) { return $local }
	$command = Get-Command emulator -ErrorAction SilentlyContinue
	if ($command) { return $command.Source }
	return $null
}

# ---------- Разбор export_presets.cfg ----------

# Возвращает текст блока пресета: строки [preset.N] и [preset.N.options].
function Get-PresetBlock([string]$Path, [string]$Name) {
	if (!(Test-Path -LiteralPath $Path)) { Fail "Не найден файл пресетов: $Path" }
	$lines = Get-Content -LiteralPath $Path -Encoding UTF8
	for ($i = 0; $i -lt $lines.Count; $i++) {
		if ($lines[$i] -notmatch '^\[preset\.(\d+)\]\s*$') { continue }
		$index = [int]$Matches[1]
		$presetName = $null
		for ($j = $i + 1; $j -lt $lines.Count; $j++) {
			if ($lines[$j] -match '^\[preset\.') { break }
			if ($lines[$j] -match '^name="(.*)"$') { $presetName = $Matches[1]; break }
		}
		if ($presetName -ne $Name) { continue }
		$block = @()
		for ($j = $i; $j -lt $lines.Count; $j++) {
			if ($j -gt $i -and $lines[$j] -match '^\[') {
				if ($lines[$j] -notmatch '^\[preset\.(\d+)' -or [int]$Matches[1] -ne $index) { break }
			}
			$block += $lines[$j]
		}
		return ($block -join "`n")
	}
	return $null
}

function Get-PresetValue([string]$Text, [string]$Key) {
	$match = [regex]::Match($Text, "(?m)^$([regex]::Escape($Key))=(.*)$")
	if (!$match.Success) { return $null }
	return $match.Groups[1].Value.Trim().Trim('"')
}

# ABI устройства -> ключ архитектуры в пресете.
$script:AbiToKey = @{
	"arm64-v8a"   = "architectures/arm64-v8a"
	"armeabi-v7a" = "architectures/armeabi-v7a"
	"x86_64"      = "architectures/x86_64"
	"x86"         = "architectures/x86"
}

# Пресет для x86_64-эмулятора: подсказываем его, когда архитектура не совпала с устройством.
$script:EmulatorPresetName = "Android Emulator"


# ---------- Устройство и эмулятор ----------

function Get-ConnectedDevice([string]$AdbPath) {
	$result = Invoke-Native $AdbPath @("devices")
	foreach ($line in ($result.Output -split "`r?`n")) {
		if ($line -match '^(\S+)\s+device\s*$') { return $Matches[1] }
	}
	return $null
}

function Wait-ForDevice([string]$AdbPath, [int]$TimeoutSec = 180) {
	$deadline = (Get-Date).AddSeconds($TimeoutSec)
	while ((Get-Date) -lt $deadline) {
		$device = Get-ConnectedDevice -AdbPath $AdbPath
		if ($device) {
			$booted = (Invoke-Native $AdbPath @("-s", $device, "shell", "getprop", "sys.boot_completed")).Output.Trim()
			if ($booted -eq "1") { return $device }
		}
		Start-Sleep -Seconds 2
	}
	return $null
}

function Start-Avd([string]$Avd) {
	$emulator = Find-Emulator
	if (!$emulator) { Fail "Не найден emulator.exe (Android SDK -> emulator). Запустите эмулятор из Android Studio вручную." }
	if ($DryRun) { Write-Info "& `"$emulator`" -avd $Avd"; return }
	Write-Info "Запускаю: $emulator -avd $Avd"
	Start-Process -FilePath $emulator -ArgumentList @("-avd", $Avd) | Out-Null
}

function Get-AvailableAvds {
	$emulator = Find-Emulator
	if (!$emulator) { return }
	$result = Invoke-Native $emulator @("-list-avds")
	foreach ($line in ($result.Output -split "`r?`n")) {
		if ($line -and $line -notmatch '^(INFO|WARNING|ERROR)') { $line.Trim() }
	}
}

# ---------- Основной сценарий ----------

Write-Step "Проект: $root"
if (!(Test-Path -LiteralPath $presetsPath)) { Fail "В корне проекта нет export_presets.cfg." }

$presetText = Get-PresetBlock -Path $presetsPath -Name $Preset
if (!$presetText) {
	$names = (Select-String -LiteralPath $presetsPath -Pattern '^name="(.*)"$' | ForEach-Object { $_.Matches[0].Groups[1].Value }) -join ", "
	Fail "В export_presets.cfg нет пресета '$Preset'. Доступные пресеты: $names"
}

$exportPath = Get-PresetValue -Text $presetText -Key "export_path"
if (!$exportPath) { Fail "В пресете '$Preset' не задан путь экспорта (export_path)." }
$apkPath = Join-Path $root ($exportPath -replace '/', '\')
Write-Info "Пресет '$Preset' -> $apkPath"

# Основной exe вместо console-обёртки: обёртка висит на Job Object (godot#110101).
$script:UseConsoleWrapper = [bool]$ConsoleWrapper
$godotPath = Find-Godot -Explicit $Godot
$adbPath = Find-Adb -Explicit $Adb
Write-Info "Godot: $godotPath"
Write-Info "adb:   $adbPath"

$device = $null
$avdUsed = ""
if (!$NoInstall) {
	$device = Get-ConnectedDevice -AdbPath $adbPath
	if (!$device) {
		if ($AvdName) {
			$avdUsed = $AvdName
			Start-Avd -Avd $AvdName
		}
		else {
			$avds = @(Get-AvailableAvds)
			if ($avds.Count -eq 1) {
				$avdUsed = $avds[0]
				Write-Info "Устройств нет, беру единственный эмулятор: $avdUsed"
				Start-Avd -Avd $avdUsed
			}
			elseif ($DryRun) {
				Write-Notice "Пробный прогон: подключённых устройств и AVD нет - продолжаю без них."
			}
			elseif ($avds.Count -eq 0) {
				Fail "Нет подключённых устройств и не найден ни один AVD. Создайте эмулятор в Android Studio (Device Manager) или укажите -AvdName."
			}
			else {
				Fail "Нет подключённых устройств. Доступные эмуляторы: $($avds -join ', '). Запустите нужный или укажите -AvdName."
			}
		}
		if ($DryRun) {
			Write-Notice "Пробный прогон: ожидание устройства пропущено."
			$device = "<эмулятор>"
		}
		else {
			Write-Info "Жду загрузку устройства (до 3 минут)..."
			$device = Wait-ForDevice -AdbPath $adbPath
			if (!$device) { Fail "Устройство не появилось. Проверьте эмулятор: adb devices." }
		}
	}
	Write-Info "Устройство: $device"
}

# ---------- Проверка архитектуры ----------

$enabledAbis = @()
foreach ($pair in $script:AbiToKey.GetEnumerator()) {
	if ((Get-PresetValue -Text $presetText -Key $pair.Value) -eq "true") { $enabledAbis += $pair.Key }
}
Write-Info "Архитектуры в пресете: $(if ($enabledAbis.Count) { $enabledAbis -join ', ' } else { 'не выбраны' })"

# ABI цели: у подключённого устройства спрашиваем систему, в пробном прогоне - из конфигурации AVD.
$targetAbi = ""
if ($device -and $device -ne "<эмулятор>") {
	$targetAbi = (Invoke-Native $adbPath @("-s", $device, "shell", "getprop", "ro.product.cpu.abi")).Output.Trim()
}
elseif ($avdUsed) {
	$configPath = Join-Path $env:USERPROFILE ".android\avd\$avdUsed.avd\config.ini"
	if (Test-Path -LiteralPath $configPath) {
		$abiLine = Select-String -LiteralPath $configPath -Pattern '^abi\.type=(.*)$' | Select-Object -First 1
		if ($abiLine) { $targetAbi = $abiLine.Matches[0].Groups[1].Value.Trim() }
	}
}

if ($targetAbi) {
	Write-Info "ABI цели: $targetAbi"
	$requiredKey = $script:AbiToKey[$targetAbi]
	if (!$requiredKey) {
		Write-Notice "Неизвестный ABI '$targetAbi': проверьте архитектуры пресета вручную."
	}
	elseif ((Get-PresetValue -Text $presetText -Key $requiredKey) -ne "true") {
		$message = "APK собирается не под $targetAbi (в пресете '$Preset' эта архитектура выключена): установка упадёт с INSTALL_FAILED_NO_MATCHING_ABIS."
		if ($FixArch) {
			Write-Notice "$message Включаю $requiredKey (правка export_presets.cfg)."
			if (!$DryRun) {
				$content = Get-Content -LiteralPath $presetsPath -Raw -Encoding UTF8
				$pattern = "(?m)^$([regex]::Escape($requiredKey))=false$"
				if ($content -match $pattern) {
					$content = $content -replace $pattern, "$requiredKey=true"
					[System.IO.File]::WriteAllText($presetsPath, $content, (New-Object System.Text.UTF8Encoding($false)))
					$presetText = Get-PresetBlock -Path $presetsPath -Name $Preset
					Write-Info "export_presets.cfg обновлён - не забудьте закоммитить изменение."
				}
				else {
					Write-Notice "Строка $requiredKey в файле не найдена: включите архитектуру в редакторе Godot."
				}
			}
		}
		else {
			Write-Notice $message
			if ($targetAbi -eq "x86_64" -and $Preset -ne $script:EmulatorPresetName) {
				Write-Notice "Для x86_64-эмулятора в проекте есть отдельный пресет: -Preset '$script:EmulatorPresetName'."
			}
			else {
				Write-Notice "Варианты: запустить с -FixArch (скрипт включит архитектуру) или использовать устройство с ABI $targetAbi."
			}
			if (!$DryRun) { exit 1 }
		}
	}
	else {
		Write-Info "Архитектура цели включена в пресете."
	}
}



# ---------- Вспомогательные файлы ----------

# Состав APK и release-keystore разбирают отдельные файлы, чтобы этот скрипт не разрастался.
. (Join-Path $PSScriptRoot "apk_report.ps1")
. (Join-Path $PSScriptRoot "keystore.ps1")

# ---------- Release-keystore ----------

# Release-APK подписывается только отдельным ключом: debug-ключ из Editor Settings не подходит.
# Переменная называется $releaseKey, а не $releaseKeystore: PowerShell не различает регистр,
# и объект ключа молча превратился бы в строку параметра -ReleaseKeystore.
$releaseKey = $null
if ($Mode -eq "release") {
	$releaseKey = Get-ReleaseKeystore -Path $ReleaseKeystore -User $ReleaseUser -Password $ReleasePassword `
		-PresetPath (Get-PresetValue -Text $presetText -Key "keystore/release") `
		-PresetUser (Get-PresetValue -Text $presetText -Key "keystore/release_user") `
		-PresetPassword (Get-PresetValue -Text $presetText -Key "keystore/release_password")
	Write-Info "Release-keystore: $($releaseKey.Path) (алиас $($releaseKey.User), источник: $($releaseKey.Source))"
}

# ---------- Сборка APK ----------

$apkDir = Split-Path -Parent $apkPath
if (!(Test-Path -LiteralPath $apkDir)) {
	if ($DryRun) { Write-Info "New-Item -ItemType Directory -Path `"$apkDir`"" }
	else { New-Item -ItemType Directory -Path $apkDir | Out-Null }
}

$exportArg = if ($Mode -eq "release") { "--export-release" } else { "--export-debug" }
# Переменные окружения сборки: не даём dotnet publish оставить после себя серверные процессы.
$buildEnv = @{
	"MSBUILDDISABLENODEREUSE" = "1"
	"DOTNET_CLI_USE_MSBUILD_SERVER" = "0"
	"DOTNET_CLI_TELEMETRY_OPTOUT" = "1"
	"UseSharedCompilation" = "false"
}
# Release-ключ Godot читает из этих переменных: пароль не попадает в export_presets.cfg.
if ($releaseKey) {
	$buildEnv["GODOT_ANDROID_KEYSTORE_RELEASE_PATH"] = $releaseKey.Path
	$buildEnv["GODOT_ANDROID_KEYSTORE_RELEASE_USER"] = $releaseKey.User
	$buildEnv["GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD"] = $releaseKey.Password
}
$buildArguments = @("--headless", "--path", "`"$root`"", $exportArg, "`"$Preset`"", "`"$apkPath`"")

if ($NoBuild) {
	Write-Step "Сборка пропущена (-NoBuild)"
	if (!(Test-Path -LiteralPath $apkPath)) { Fail "Нет готового APK: $apkPath - запустите скрипт без -NoBuild." }
	$apkInfo = Get-Item -LiteralPath $apkPath
	Write-Info "APK: $($apkInfo.FullName)"
	Write-Info "Размер: $([math]::Round($apkInfo.Length / 1MB, 2)) МБ, изменён $($apkInfo.LastWriteTime.ToString('dd.MM HH:mm:ss'))"
	Show-ApkSizeReport -ApkPath $apkInfo.FullName
}
else {
	Write-Step "Сборка $Mode-APK пресетом '$Preset'"
	Write-Info "Экспорт C#-проекта запускает dotnet publish: это может занять несколько минут."
	$buildStart = Get-Date

	if ($DryRun) {
		Write-Info "& `"$godotPath`" --headless --path `"$root`" $exportArg `"$Preset`" `"$apkPath`""
		foreach ($key in $buildEnv.Keys) {
			# Пароль ключа в консоль не печатаем.
			$value = if ($key -match 'PASSWORD') { '***' } else { $buildEnv[$key] }
			Write-Info "env: $key=$value"
		}
	}
	else {
		$build = Invoke-BuildStreaming -FilePath $godotPath -Arguments $buildArguments -Environment $buildEnv `
			-WatchedFile $apkPath -TimeoutSec $BuildTimeout

		if ($build.TimedOut) { Fail "Сборка не завершилась за $BuildTimeout с - процесс снят." }
		if ($build.Hung) {
			Write-Notice "Godot записал APK, но не завершился (серверные процессы dotnet держат Job Object) - процесс снят."
		}
		# Главный признак успеха - APK, перезаписанный во время этого запуска.
		$apkInfo = Get-Item -LiteralPath $apkPath -ErrorAction SilentlyContinue
		if (!$apkInfo -or $apkInfo.LastWriteTime -lt $buildStart) {
			if ($build.Output -match '(?i)export template') {
				Write-Notice "Похоже, не установлены шаблоны экспорта: Editor -> Manage Export Templates -> Download and Install."
			}
			if ($build.Code) { Fail "Сборка APK не удалась (код возврата $($build.Code))." }
			Fail "Сборка APK не удалась - файл $apkPath не обновлён, смотрите вывод выше."
		}
		# Godot пишет APK до подписи: при ошибке подписи файл уже обновлён, но экспорт провален.
		if ($build.Output -match 'Project export for preset .* failed') {
			if ($releaseKey) {
				Write-Notice "Release-APK подписывается ключом $($releaseKey.Path) (источник: $($releaseKey.Source)): проверьте пароль в $($script:KeystoreCredsPath)."
			}
			Fail "Экспорт пресета '$Preset' не удался - смотрите вывод выше."
		}
		if ($build.Output -match 'Could not find version of build tools') {
			Write-Notice "Godot не нашёл свежие build-tools и подписал APK старой версией. Обновите Android SDK Build-Tools в SDK Manager."
		}
		Write-Info "APK: $($apkInfo.FullName)"
		Write-Info "Размер: $([math]::Round($apkInfo.Length / 1MB, 2)) МБ, изменён $($apkInfo.LastWriteTime.ToString('HH:mm:ss'))"
		Show-ApkSizeReport -ApkPath $apkInfo.FullName
	}
}

# ---------- Установка и запуск ----------

$package = ""
if (!$NoInstall) {
	$uniqueName = Get-PresetValue -Text $presetText -Key "package/unique_name"
	$projectNameLine = Select-String -LiteralPath (Join-Path $root "project.godot") -Pattern '^config/name="(.*)"$' | Select-Object -First 1
	$genName = ""
	if ($projectNameLine) { $genName = ($projectNameLine.Matches[0].Groups[1].Value -replace '[^A-Za-z0-9]', '').ToLower() }
	if ($uniqueName) { $package = $uniqueName.Replace('$genname', $genName).ToLower() }

	if ($DryRun) {
		Write-Step "Установка (пробный прогон)"
		Write-Info "& `"$adbPath`" -s $device install -r `"$apkPath`""
		if (!$NoLaunch) { Write-Info "& `"$adbPath`" -s $device shell monkey -p $package -c android.intent.category.LAUNCHER 1" }
	}
	else {
		Write-Step "Установка APK на $device"
		$install = Invoke-Native $adbPath @("-s", $device, "install", "-r", $apkPath)
		$installOutput = $install.Output
		Write-Info $installOutput.Trim()
		if ($installOutput -match '(?i)Failure') {
			if ($installOutput -match 'INSTALL_FAILED_NO_MATCHING_ABIS') {
				Write-Notice "Устройство не поддерживает архитектуры APK: пересоберите с -FixArch или пресетом '$script:EmulatorPresetName' для x86_64-эмулятора."
			}
			elseif ($installOutput -match 'INSTALL_FAILED_UPDATE_INCOMPATIBLE|signatures do not match') {
				Write-Notice "Ранее установленное приложение подписано другим ключом, удалите его:"
				Write-Info "& `"$adbPath`" -s $device uninstall $package"
			}
			Fail "Установка не удалась."
		}

		# Уточняем имя пакета по устройству: $genname у Godot формируется из имени проекта.
		if ($package) {
			$installed = (Invoke-Native $adbPath @("-s", $device, "shell", "pm", "list", "packages")).Output
			if ($installed -notmatch [regex]::Escape($package) -and $genName) {
				$match = [regex]::Match($installed, "package:(\S*$([regex]::Escape($genName))\S*)", 'IgnoreCase')
				if ($match.Success) { $package = $match.Groups[1].Value }
			}
		}

		if (!$NoLaunch) {
			Write-Step "Запуск приложения"
			$launch = Invoke-Native $adbPath @("-s", $device, "shell", "monkey", "-p", $package, "-c", "android.intent.category.LAUNCHER", "1")
			if ($launch.Output -match '(?i)No activities found|Error:') {
				Write-Notice "Не удалось запустить $package автоматически - откройте приложение на устройстве вручную."
			}
			else {
				Write-Info "Запущено: $package"
			}
		}
	}
}

# ---------- Логи ----------

if ($Logs -and $device) {
	Write-Step "Лог игры (тег godot)"
	if ($DryRun) {
		Write-Info "& `"$adbPath`" -s $device logcat -d -s godot:V"
	}
	else {
		Start-Sleep -Seconds 5
		$log = (Invoke-Native $adbPath @("-s", $device, "logcat", "-d", "-s", "godot:V")).Output
		$lines = @($log -split "`r?`n" | Where-Object { $_ })
		$tail = if ($lines.Count -gt 40) { $lines[($lines.Count - 40)..($lines.Count - 1)] } else { $lines }
		foreach ($line in $tail) { Write-Info $line }
		Write-Info "Живой лог: & `"$adbPath`" -s $device logcat -s godot:V"
	}
}

# ---------- Итог ----------

Write-Step "Готово"
Write-Info "APK:        $apkPath"
if ($device) { Write-Info "Устройство: $device" }
if ($package) { Write-Info "Пакет:      $package" }
if ($DryRun) { Write-Notice "Пробный прогон: сборка, установка и запуск не выполнялись." }
