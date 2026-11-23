# 🛠️ Windows Maintenance Tool (CMD) / Herramienta de Mantenimiento de Windows

[🇺🇸 English](#-english) | [🇪🇸 Español](#-español)

---

## 🇺🇸 English

Lightweight interactive Batch (`.bat`) script designed to optimize system performance and free up disk space on Windows 10 and Windows 11 without installing third-party software.

### ✨ Features

* **Auto-Elevation (UAC):** Automatically detects if it lacks administrator privileges and prompts for UAC elevation.
* **RAM Optimization:** Flushes background process *Working Sets* using the native Windows `psapi.dll` API without closing active applications.
* **Temporary Files Cleanup:** Removes leftover files from `%TEMP%`, `C:\Windows\Temp`, and `C:\Windows\Prefetch`.
* **Recycle Bin Purge:** Completely clears the `$Recycle.Bin` directory.
* **Windows Update Cache Cleanup:** Safely stops `wuauserv` and `bits` services, deletes old update downloads, and restarts the services.
* **Network/DNS Flush:** Runs `ipconfig /flushdns` to refresh the DNS resolver cache.
* **All-in-One Mode:** Executes all maintenance tasks sequentially in a single run.

### 🚀 How to Download and Use

1. Click on the **`MantenimientoSistema.bat`** file in the repository file list above.
2. Click the **Download raw file** button (download icon on the top right) or copy the raw code and save it as `MantenimientoSistema.bat`.
3. **Double-click** the downloaded `.bat` file.
4. Click **Yes** on the **User Account Control (UAC)** prompt to grant administrator permissions.
5. Type a number from `1` to `7` in the console menu and press **Enter**.

### ⚠️ Security Notes

* If Windows Defender SmartScreen displays a *"Windows protected your PC"* warning when running a downloaded `.bat` file, click **More info** > **Run anyway**.
* Any temporary files currently locked or in use by Windows are safely skipped to prevent system errors.

---

## 🇪🇸 Español

Script interactivo en Batch (`.bat`) diseñado para optimizar el rendimiento y liberar espacio en disco en sistemas Windows 10 y Windows 11 sin necesidad de instalar programas de terceros.

### ✨ Características

* **Auto-elevación de privilegios (UAC):** Detecta si se ejecuta sin permisos de administrador y solicita confirmación automáticamente.
* **Liberación de Memoria RAM:** Vacía los *Working Sets* de procesos en segundo plano mediante la API nativa `psapi.dll` sin cerrar tus aplicaciones abiertas.
* **Limpieza de Archivos Temporales:** Elimina archivos residuales de `%TEMP%`, `C:\Windows\Temp` y `C:\Windows\Prefetch`.
* **Vaciado de Papelera de Reciclaje:** Limpia por completo el directorio `$Recycle.Bin`.
* **Limpieza de Windows Update:** Detiene de forma segura los servicios `wuauserv` y `bits`, elimina la caché de descargas antiguas y reinicia los servicios.
* **Limpieza de Red/DNS:** Ejecuta `ipconfig /flushdns` para renovar la caché de resolución de nombres.
* **Modo Todo en Uno:** Ejecuta todas las tareas anteriores en secuencia con una sola opción.

### 🚀 Cómo Descargar y Usar

1. Haz clic en el archivo **`MantenimientoSistema.bat`** en la lista superior de este repositorio.
2. Haz clic en el botón **Download raw file** (icono de descarga arriba a la derecha) o copia el código y guárdalo como `MantenimientoSistema.bat`.
3. Haz **doble clic** sobre el archivo `.bat` descargado.
4. Haz clic en **Sí** en el aviso de **Control de cuentas de usuario (UAC)** para otorgar permisos de administrador.
5. Escribe un número del `1` al `7` en el menú de la consola y presiona **Enter**.

### ⚠️ Notas de Seguridad

* Si Windows Defender SmartScreen muestra una advertencia de *"Windows protegió su PC"* al ejecutar un archivo `.bat` descargado de internet, haz clic en **Más información** > **Ejecutar de todas formas**.
* El script omite automáticamente cualquier archivo temporal que esté siendo utilizado por Windows para evitar errores en el sistema.

---

## 📄 License / Licencia

Free to use for personal and technical maintenance. / Uso libre para mantenimiento personal y soporte técnico.
