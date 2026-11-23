# 🛠️ Optimizador y Mantenimiento de Windows (CMD & EXE)

[🇪🇸 Español](#-español) | [🇺🇸 English](#-english)

---

## 🇪🇸 Español

Herramienta ligera en Batch (`.bat`) y ejecutable nativo de Windows (`.exe`) diseñada para optimizar el rendimiento y liberar espacio en Windows 10 y Windows 11 sin instalar programas externos.

### ✨ Características
* **Auto-elevación de privilegios (UAC):** Solicita permisos de administrador automáticamente al abrirse.
* **Liberación de Memoria RAM:** Vacía los *Working Sets* de procesos en segundo plano mediante la API nativa `psapi.dll` sin cerrar tus programas abiertos.
* **Limpieza de Archivos Temporales:** Elimina archivos residuales de `%TEMP%`, `C:\Windows\Temp` y `C:\Windows\Prefetch`.
* **Vaciado de Papelera de Reciclaje:** Limpia por completo el directorio `$Recycle.Bin`.
* **Limpieza de Windows Update:** Detiene de forma segura `wuauserv` y `bits`, borra la caché de descargas antiguas y reinicia los servicios.
* **Limpieza de Red/DNS:** Ejecuta `ipconfig /flushdns`.
* **Modo Todo en Uno:** Ejecuta todas las tareas en secuencia con una sola opción.

### 🚀 Cómo Descargar y Usar
1. **Opción A (Ejecutable con icono):** Haz clic en **[`MantenimientoSistema.exe`](https://github.com/ricodaniel/optimizador-windows-cmd/raw/main/MantenimientoSistema.exe)** para descargarlo directamente y haz doble clic sobre él.
2. **Opción B (Script en texto plano):** Descarga **[`MantenimientoSistema.bat`](https://github.com/ricodaniel/optimizador-windows-cmd/raw/main/MantenimientoSistema.bat)** y ejecútalo con doble clic.
3. Acepta el aviso de **Control de cuentas de usuario (Sí)** y elige una opción del `1` al `7` en el menú.

---

## 🇺🇸 English

Lightweight interactive Batch (`.bat`) and native Windows executable (`.exe`) tool designed to optimize system performance and free up disk space on Windows 10 and Windows 11 without third-party software.

### ✨ Features
* **Auto-Elevation (UAC):** Automatically requests administrator privileges on startup.
* **RAM Optimization:** Flushes background process *Working Sets* using the native `psapi.dll` API without closing active applications.
* **Temporary Files Cleanup:** Removes residual files from `%TEMP%`, `C:\Windows\Temp`, and `C:\Windows\Prefetch`.
* **Recycle Bin Purge:** Completely clears `$Recycle.Bin`.
* **Windows Update Cache Cleanup:** Safely stops `wuauserv` and `bits`, deletes old update downloads, and restarts the services.
* **Network/DNS Flush:** Runs `ipconfig /flushdns`.
* **All-in-One Mode:** Executes all tasks sequentially in a single step.

### 🚀 How to Download and Use
1. **Option A (Executable):** Click **[`MantenimientoSistema.exe`](https://github.com/ricodaniel/optimizador-windows-cmd/raw/main/MantenimientoSistema.exe)** to download it directly and double-click to run.
2. **Option B (Batch Script):** Download **[`MantenimientoSistema.bat`](https://github.com/ricodaniel/optimizador-windows-cmd/raw/main/MantenimientoSistema.bat)** and double-click it.
3. Click **Yes** on the **User Account Control (UAC)** prompt and select an option from `1` to `7`.
