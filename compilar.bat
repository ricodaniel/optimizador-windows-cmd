@echo off
setlocal EnableDelayedExpansion
title Compilador Nativo - WinOpt Studio

echo =====================================================================
echo                WinOpt Studio - Compilador Nativo
echo                Compilacion con .NET Framework csc.exe
echo =====================================================================
echo.

REM 1. Localizar compilador csc.exe de .NET Framework 4.x
set "CSC="
if exist "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
    set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    echo [*] Encontrado compilador csc.exe 64-bit.
) else (
    if exist "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe" (
        set "CSC=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
        echo [*] Encontrado compilador csc.exe 32-bit.
    )
)

if not defined CSC (
    echo [ERROR] No se encontro csc.exe de .NET Framework v4.0.30319 en el sistema.
    echo Asegurate de que .NET Framework 4.5 o superior este instalado.
    goto :FAIL
)

echo [*] Ruta del compilador: "!CSC!"

REM 2. Validar archivos de origen
if not exist "WinOptStudio.cs" (
    echo [ERROR] No se encontro el archivo de codigo fuente 'WinOptStudio.cs'.
    goto :FAIL
)

set "ICON_PARAM="
if exist "WinOptStudio.ico" (
    echo [*] Icono detectado: WinOptStudio.ico
    set "ICON_PARAM=/win32icon:WinOptStudio.ico"
) else (
    echo [ADVERTENCIA] WinOptStudio.ico no encontrado. Se usara el icono por defecto.
)

set "MANIFEST_PARAM="
if exist "app.manifest" (
    echo [*] Manifiesto UAC detectado: app.manifest
    set "MANIFEST_PARAM=/win32manifest:app.manifest"
) else (
    echo [ADVERTENCIA] 'app.manifest' no encontrado. La compilacion continuara sin manifiesto.
)

echo.
echo [*] Compilando WinOptStudio.exe...
echo.

REM 3. Compilar ejecutable de Windows (winexe) con optimizaciones
"!CSC!" /nologo /target:winexe /out:WinOptStudio.exe /optimize+ /platform:anycpu /filealign:512 !MANIFEST_PARAM! !ICON_PARAM! /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Core.dll WinOptStudio.cs

if errorlevel 1 (
    echo.
    echo [AVISO] WinOptStudio.exe esta bloqueado por una instancia abierta.
    echo [*] Generando compilacion limpia en WinOptStudio_v4.exe...
    "!CSC!" /nologo /target:winexe /out:WinOptStudio_v4.exe /optimize+ /platform:anycpu /filealign:512 !MANIFEST_PARAM! !ICON_PARAM! /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Core.dll WinOptStudio.cs
    if errorlevel 1 goto :FAIL
    set "EXE_NAME=WinOptStudio_v4.exe"
) else (
    set "EXE_NAME=WinOptStudio.exe"
)

echo.
echo =====================================================================
echo [EXITO] Compilacion completada con exito.
echo Archivo generado: !EXE_NAME!
echo Manifiesto UAC incrustado (solicitara permisos de Administrador).
echo =====================================================================
echo.
pause
exit /b 0

:FAIL
echo.
echo =====================================================================
echo [FALLO] No se pudo generar WinOptStudio.exe
echo =====================================================================
echo.
pause
exit /b 1
