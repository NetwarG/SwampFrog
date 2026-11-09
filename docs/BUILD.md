# Сборка и запуск

← [Назад к README](../README.md)

## Требования

- Godot **4.7.2 Mono** (C#);
- .NET **8.0** SDK (для Android — .NET **9** SDK: в `SwampFrog.csproj` под Android указан `net9.0`).

## Запуск

1. Открыть проект в редакторе Godot и нажать Play, либо
2. собрать и запустить из командной строки:

```bash
dotnet build
godot --path .
```

Главная сцена — `Scenes/GameScreen.tscn`. Она создаёт `Scenes/Main.tscn` внутри игрового мира с
шириной 540 и высотой, равной высоте экрана, а интерфейс размещает на той же колонке поверх поля.

## Физика: Rapier2D

В проекте используется **Godot Rapier 2D 0.35.2** (`addons/godot-rapier2d`), в `project.godot` выбран
`physics/2d/physics_engine="Rapier2D"`. Каталог расширения исключён из Git, поэтому в свежей копии он
пуст и физика идёт на **встроенном движке Godot** — для игры и тестов этого достаточно.

Чтобы использовать Rapier (в том числе на Android), нужно положить файлы расширения в
`addons/godot-rapier2d/bin/`, включая `.so` под `arm64-v8a` и `x86_64`.

## Сборка и запуск на Android

В `export_presets.cfg` настроены два пресета: **Android** (`build/SwampFrog.apk`, `arm64-v8a` —
телефоны и планшеты) и **Android Emulator** (`build/SwampFrog-emulator.apk`, `x86_64` — эмуляторы).
Сборку, установку и запуск делает скрипт в `Tools/`:

```powershell
Tools\build_android.ps1                     # собрать, установить и запустить
Tools\build_android.ps1 -AvdName Pixel_10 -Logs   # поднять эмулятор и показать лог игры
Tools\build_android.ps1 -Preset "Android Emulator" -AvdName Pixel_10   # x86_64-APK для эмулятора
Tools\build_android.ps1 -Mode release             # release-APK для телефона
Tools\build_android.ps1 -Mode release -NoInstall -NoLaunch   # только собрать release-APK
Tools\build_android.ps1 -NoInstall -NoLaunch      # только собрать APK
Tools\build_android.ps1 -NoBuild                  # поставить и запустить уже собранный APK
Tools\build_android.ps1 -DryRun                   # проверить окружение и показать команды
```

`Tools\build_android.bat` — та же команда для двойного клика. Скрипт находит Godot и `adb` (или берёт
пути из `-Godot`/`-Adb`), читает путь экспорта из пресета, при необходимости поднимает эмулятор,
собирает APK (`--export-debug`, релиз — `-Mode release`), печатает состав APK (`Tools/apk_report.ps1`),
ставит его через `adb install -r` и запускает.

Ключи: `-NoInstall`, `-NoLaunch`, `-NoBuild`, `-Logs`, `-DryRun`, `-FixArch`, `-BuildTimeout` (предел
времени сборки, по умолчанию 900 с), `-ConsoleWrapper`,
`-ReleaseKeystore`/`-ReleaseUser`/`-ReleasePassword`.

**Почему сборка может выглядеть зависшей.** Скрипт экспортирует основным исполняемым файлом Godot
(`Godot_vX_win64.exe`), а не `*_console.exe`: console-обёртка запускает движок внутри Job Object и ждёт
завершения всего дерева процессов, а экспорт C#-проекта вызывает `dotnet publish`, который оставляет
после себя серверные процессы (MSBuild-ноды, Roslyn `VBCSCompiler`), поэтому обёртка продолжает висеть
уже после записи APK ([godotengine/godot#110101](https://github.com/godotengine/godot/issues/110101)).
Если процесс Godot всё же не завершится после записи APK, скрипт снимет его дерево процессов и
продолжит установку. Ключ `-ConsoleWrapper` возвращает прежнее поведение.

**Архитектура.** Пресет **Android** собирает только `arm64-v8a`, пресет **Android Emulator** — только
`x86_64`. Скрипт сверяет архитектуру с ABI устройства (`adb shell getprop ro.product.cpu.abi`): при
несовпадении он предупредит про `INSTALL_FAILED_NO_MATCHING_ABIS` и предложит пресет для эмулятора или
ключ `-FixArch`.

## Размер APK

APK на C# тяжёлый из-за рантайма Mono и шаблона движка, поэтому в сборку попадает только нужное:

| Что настроено | Почему |
|---|---|
| Одна архитектура на пресет | `lib/<abi>/*.so` и `assets/.godot/mono/publish/<abi>` стоят ~40 МБ на каждую ABI |
| Без статических архивов `.a` | Godot 4.7 копирует их из `dotnet publish`, хотя в рантайме они не нужны ([godot#115679](https://github.com/godotengine/godot/issues/115679), исправлено в 4.8); исключает цель `StripMonoStaticLibraries` в `SwampFrog.csproj` |
| `dotnet/include_debug_symbols=false` | `.pdb` не уезжают в APK |
| `dotnet/include_scripts_content=false` | исходники `.cs` не уезжают в APK |
| `exclude_filter="Tests/*"` | тестовые сцены не попадают в сборку |

Итог по пресету **Android**: было ~200 МиБ (две архитектуры, с `.a`), стало ~45 МБ.
`Tools/build_android.ps1` печатает такой разбор после каждой сборки и предупреждает, если `.a` вернулись.

## Release-подпись

Ключ `-Mode release` собирает релизный шаблон движка (ещё на несколько МБ меньше) и подписывает APK
отдельным release-ключом: debug-ключ из Editor Settings для релиза не подходит, а без ключа Godot
падает на шаге подписи. Ключ скрипт ищет в `-ReleaseKeystore`, переменных окружения
`GODOT_ANDROID_KEYSTORE_RELEASE_PATH`/`_USER`/`_PASSWORD`, ключах `keystore/release*` пресета и в
локальном файле `Tools/release_keystore.local.json`, а если ключа нет совсем — создаёт
`%APPDATA%\Godot\keystores\release.keystore` через `keytool` и сохраняет пароль в тот же локальный файл.

Проверка идёт до сборки, поэтому о проблеме с ключом видно сразу, а не после нескольких минут экспорта.
Пароль в `export_presets.cfg` не пишется: скрипт передаёт его Godot переменной окружения, а локальный
файл с кредами исключён из Git — храните его отдельно, подпись нельзя менять между обновлениями APK.

```powershell
Tools\build_android.ps1 -Mode release                       # собрать release-APK, установить и запустить
Tools\build_android.ps1 -Mode release -NoInstall -NoLaunch   # только собрать build\SwampFrog.apk
Tools\build_android.ps1 -Mode release -Logs                  # собрать и показать лог игры
```

## Что нужно установить для Android

Editor Settings → Export → Android: Android SDK Path и Java SDK Path.

- JDK 17 (в 4.8+ Godot настраивает SDK сам, в 4.7 пути задаются вручную);
- Android SDK: `platform-tools`, свежие `build-tools` и `platform` — иначе Godot подпишет APK старой
  версией и предупредит об этом;
- .NET SDK 9 для Android;
- шаблоны экспорта: Editor → Manage Export Templates;
- `keytool` из JDK для `-Mode release` (debug-подпись берётся из Editor Settings → Export → Android).

---

← [Назад к README](../README.md)
