<#
.SYNOPSIS
	Подготовка release-keystore для сборки Android-версии SwampFrog.

.DESCRIPTION
	Godot подписывает release-APK только ключом из пресета или из переменных окружения
	GODOT_ANDROID_KEYSTORE_RELEASE_PATH, _USER и _PASSWORD: debug-ключ из Editor Settings
	для релиза не подходит, и без ключа экспорт падает на шаге подписи с ошибкой
	"Не удалось найти хранилище ключей релиза".

	Файл подключается к Tools\build_android.ps1 и пользуется его функциями вывода
	(Write-Info, Write-Notice, Fail) и запуска внешних программ (Invoke-Native).
	Ключ ищется по порядку: параметры скрипта, переменные окружения, ключи
	keystore/release* пресета, локальный файл Tools\release_keystore.local.json.
	Если ключа нет совсем, он создаётся через keytool, а путь, алиас и пароль
	остаются в локальном файле - в Git он не попадает, export_presets.cfg не правится.

.EXAMPLE
	$keystore = Get-ReleaseKeystore
	Найти release-keystore для сборки, создав его при полном отсутствии.
#>

# Ключ по умолчанию лежит рядом с debug-ключом Godot, вне репозитория.
$script:ReleaseKeystorePath = Join-Path $env:APPDATA "Godot\keystores\release.keystore"

# Локальный файл с путём, алиасом и паролем ключа.
$script:KeystoreCredsPath = Join-Path $PSScriptRoot "release_keystore.local.json"

# Алиас ключа по умолчанию.
$script:ReleaseKeystoreAlias = "swampfrog"

# Возвращает значение настройки редактора Godot (Editor Settings) или $null.
function Get-EditorSetting([string]$Key) {
	$settingsFile = Get-ChildItem -Path (Join-Path $env:APPDATA "Godot") -Filter "editor_settings-*.tres" -File -ErrorAction SilentlyContinue |
		Sort-Object Name -Descending | Select-Object -First 1
	if (!$settingsFile) { return $null }
	$line = Select-String -LiteralPath $settingsFile.FullName -Pattern "^$([regex]::Escape($Key))\s*=\s*`"(.*)`"$" | Select-Object -First 1
	if (!$line) { return $null }
	# В .tres обратные слеши экранированы.
	return $line.Matches[0].Groups[1].Value.Replace('\\', '\')
}

# Возвращает путь к keytool из Java SDK, настроенного в Godot.
function Find-Keytool([string]$JavaSdk) {
	if (!$JavaSdk) { $JavaSdk = Get-EditorSetting "export/android/java_sdk_path" }
	if ($JavaSdk) {
		$candidate = Join-Path $JavaSdk "bin\keytool.exe"
		if (Test-Path -LiteralPath $candidate) { return $candidate }
	}
	$command = Get-Command keytool -ErrorAction SilentlyContinue
	if ($command) { return $command.Source }
	Fail "Не найден keytool. Задайте Java SDK в Editor Settings -> Export -> Android (Java SDK Path) или добавьте keytool в PATH."
}

# Читает путь, алиас и пароль ключа из локального файла или возвращает $null.
function Get-LocalKeystoreCreds {
	if (!(Test-Path -LiteralPath $script:KeystoreCredsPath)) { return $null }
	try { return (Get-Content -LiteralPath $script:KeystoreCredsPath -Raw -Encoding UTF8 | ConvertFrom-Json) }
	catch {
		Write-Notice "Не удалось прочитать $($script:KeystoreCredsPath): проверьте файл вручную."
		return $null
	}
}

# Сохраняет путь, алиас и пароль ключа в локальный файл вне Git.
function Save-LocalKeystoreCreds([string]$Path, [string]$User, [string]$Password) {
	$json = ([ordered]@{ path = $Path; user = $User; password = $Password } | ConvertTo-Json)
	[System.IO.File]::WriteAllText($script:KeystoreCredsPath, $json, (New-Object System.Text.UTF8Encoding($false)))
}

# Проверяет, что ключ существует и пароль с алиасом подходят.
function Test-ReleaseKeystore([string]$Path, [string]$User, [string]$Password) {
	if (!$Path -or !(Test-Path -LiteralPath $Path)) { return $false }
	# Без пароля keytool спросит его в консоли и повиснет: ограничиваемся проверкой файла.
	if (!$Password) { return $true }
	$arguments = @("-list", "-keystore", $Path, "-storepass", $Password)
	if ($User) { $arguments += @("-alias", $User) }
	return (Invoke-Native (Find-Keytool) $arguments).Code -eq 0
}

# Создаёт новый release-keystore и сохраняет его параметры локально.
function New-ReleaseKeystore([string]$Path, [string]$User, [string]$Password) {
	if (!$Path) { $Path = $script:ReleaseKeystorePath }
	if (!$User) { $User = $script:ReleaseKeystoreAlias }
	if (!$Password) { $Password = [guid]::NewGuid().ToString("N") }
	$directory = Split-Path -Parent $Path
	if (!(Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
	Write-Info "Создаю release-keystore: $Path (алиас $User)"
	$result = Invoke-Native (Find-Keytool) @(
		"-genkeypair", "-keystore", $Path, "-alias", $User, "-keyalg", "RSA", "-keysize", "2048",
		"-validity", "10000", "-storepass", $Password, "-keypass", $Password, "-dname", "CN=SwampFrog, O=Froggers"
	)
	if ($result.Code -ne 0 -or !(Test-Path -LiteralPath $Path)) {
		Write-Info $result.Output.Trim()
		Fail "keytool не создал ключ $Path (код возврата $($result.Code))."
	}
	Save-LocalKeystoreCreds -Path $Path -User $User -Password $Password
	Write-Notice "Параметры ключа сохранены в $($script:KeystoreCredsPath): файл вне Git, храните его отдельно - подпись нельзя менять между обновлениями APK."
	return [pscustomobject]@{ Path = $Path; User = $User; Password = $Password; Source = "новый ключ" }
}

# Возвращает release-keystore для сборки, создавая его при полном отсутствии.
function Get-ReleaseKeystore {
	param(
		[string]$Path = "",
		[string]$User = "",
		[string]$Password = "",
		[string]$PresetPath = "",
		[string]$PresetUser = "",
		[string]$PresetPassword = ""
	)
	$local = Get-LocalKeystoreCreds
	$candidates = @(
		@{ Path = $Path; User = $User; Password = $Password; Source = "параметры скрипта" },
		@{ Path = $env:GODOT_ANDROID_KEYSTORE_RELEASE_PATH; User = $env:GODOT_ANDROID_KEYSTORE_RELEASE_USER; Password = $env:GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD; Source = "переменные окружения" },
		@{ Path = $PresetPath; User = $PresetUser; Password = $PresetPassword; Source = "пресет" },
		@{ Path = $local.path; User = $local.user; Password = $local.password; Source = "локальный файл" }
	)
	$broken = ""
	foreach ($candidate in $candidates) {
		if (!$candidate.Path) { continue }
		if (!(Test-Path -LiteralPath $candidate.Path)) {
			Write-Notice "Release-keystore из источника '$($candidate.Source)' не найден: $($candidate.Path)"
			$broken = $candidate.Path
			continue
		}
		if (!(Test-ReleaseKeystore -Path $candidate.Path -User $candidate.User -Password $candidate.Password)) {
			Write-Notice "Release-keystore $($candidate.Path) есть, но пароль или алиас '$($candidate.User)' не подходят."
			$broken = $candidate.Path
			continue
		}
		return [pscustomobject]@{ Path = $candidate.Path; User = $candidate.User; Password = $candidate.Password; Source = $candidate.Source }
	}
	if ($broken) {
		Fail "Release-keystore $broken найден, но не открылся. Поправьте пароль в $($script:KeystoreCredsPath) или удалите файл, чтобы скрипт создал новый ключ."
	}
	return New-ReleaseKeystore
}
