@echo off
rem Обёртка для двойного клика: запускает Tools\build_android.ps1 в PowerShell.
rem Аргументы передаются дальше, например: build_android.bat -AvdName Pixel_9 -Logs
chcp 65001 >nul
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_android.ps1" %*
set EXITCODE=%ERRORLEVEL%
echo.
if not "%EXITCODE%"=="0" (
	echo Скрипт завершился с кодом %EXITCODE%.
) else (
	echo Скрипт завершён успешно.
)
pause
exit /b %EXITCODE%
