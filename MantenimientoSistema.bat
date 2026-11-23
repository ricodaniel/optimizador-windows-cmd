@echo off
title Herramienta de Mantenimiento de Windows (CMD)
color 0B

:: ==============================================================================
:: 1. VERIFICACION Y AUTO-ELEVACION DE PRIVILEGIOS (UAC)
:: ==============================================================================
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [!] Se requieren permisos de Administrador. Solicitando elevacion...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

:: ==============================================================================
:: 2. MENU PRINCIPAL
:: ==============================================================================
:MENU
cls
echo =======================================================
echo        HERRAMIENTA DE MANTENIMIENTO DE WINDOWS (CMD)
echo =======================================================
echo.
echo   [1] Liberar Memoria RAM en cache (Working Sets)
echo   [2] Limpiar Archivos Temporales (TEMP, Windows\Temp, Prefetch)
echo   [3] Vaciar Papelera de Reciclaje
echo   [4] Limpiar Cache de Windows Update
echo   [5] Limpiar Cache de DNS y Red
echo   [6] Ejecutar TODO (Mantenimiento Completo)
echo   [7] Salir
echo.
echo =======================================================
set /p opcion="Selecciona una opcion (1-7): "

if "%opcion%"=="1" goto RAM
if "%opcion%"=="2" goto TEMP
if "%opcion%"=="3" goto PAPELERA
if "%opcion%"=="4" goto WUPDATE
if "%opcion%"=="5" goto DNS
if "%opcion%"=="6" goto TODO
if "%opcion%"=="7" goto SALIR

echo.
echo [!] Opcion no valida. Intenta de nuevo.
timeout /t 2 >nul
goto MENU

:: ==============================================================================
:: 3. MODULOS DE LIMPIEZA
:: ==============================================================================

:RAM
echo.
echo [*] Liberando memoria RAM en segundo plano...
powershell -NoProfile -Command "$code = '[DllImport(\"psapi.dll\")] public static extern bool EmptyWorkingSet(IntPtr hProcess);'; $type = Add-Type -MemberDefinition $code -Name 'Mem' -Namespace 'Win32' -PassThru; Get-Process | ForEach-Object { try { $null = $type::EmptyWorkingSet($_.Handle) } catch {} }"
echo [+] Memoria RAM optimizada correctamente.
if "%modo_todo%"=="1" goto :eof
pause
goto MENU

:TEMP
echo.
echo [*] Limpiando archivos temporales de usuario y sistema...
for /d %%p in ("%TEMP%\*") do rmdir "%%p" /s /q >nul 2>&1
for %%f in ("%TEMP%\*") do if /i not "%%~nxf"=="%~nx0" del /f /q "%%f" >nul 2>&1

del /s /f /q "C:\Windows\Temp\*.*" >nul 2>&1
for /d %%p in ("C:\Windows\Temp\*") do rmdir "%%p" /s /q >nul 2>&1

del /s /f /q "C:\Windows\Prefetch\*.*" >nul 2>&1
echo [+] Archivos temporales eliminados.
if "%modo_todo%"=="1" goto :eof
pause
goto MENU

:PAPELERA
echo.
echo [*] Vaciando la Papelera de Reciclaje...
rd /s /q %systemdrive%\$Recycle.Bin >nul 2>&1
echo [+] Papelera vaciada correctamente.
if "%modo_todo%"=="1" goto :eof
pause
goto MENU

:WUPDATE
echo.
echo [*] Deteniendo servicios de Windows Update...
net stop wuauserv >nul 2>&1
net stop bits >nul 2>&1

echo [*] Eliminando cache de descargas...
del /s /f /q "C:\Windows\SoftwareDistribution\Download\*.*" >nul 2>&1
for /d %%p in ("C:\Windows\SoftwareDistribution\Download\*") do rmdir "%%p" /s /q >nul 2>&1

echo [*] Reiniciando servicios de Windows Update...
net start wuauserv >nul 2>&1
net start bits >nul 2>&1
echo [+] Cache de Windows Update limpiada.
if "%modo_todo%"=="1" goto :eof
pause
goto MENU

:DNS
echo.
echo [*] Vaciando cache de resolucion DNS...
ipconfig /flushdns >nul
echo [+] Cache DNS vaciada correctamente.
if "%modo_todo%"=="1" goto :eof
pause
goto MENU

:TODO
cls
echo =======================================================
echo          INICIANDO MANTENIMIENTO COMPLETO
echo =======================================================
set modo_todo=1
call :TEMP
call :PAPELERA
call :WUPDATE
call :DNS
call :RAM
set modo_todo=0
echo.
echo =======================================================
echo     MANTENIMIENTO COMPLETO FINALIZADO CON EXITO
echo =======================================================
pause
goto MENU

:SALIR
echo.
echo Saliendo de la herramienta...
timeout /t 1 >nul
exit
