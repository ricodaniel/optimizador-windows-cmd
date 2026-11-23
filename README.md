# ⚡ WinOpt Studio — Suite Nativa de Rendimiento y Mantenimiento para Windows

![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011%20(64--bit)-0078D6?style=for-the-badge&logo=windows&logoColor=white)
![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8%20Nativo-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Language](https://img.shields.io/badge/C%23%20%26%20Batch-Sin%20Dependencias-2EA043?style=for-the-badge)

[🇪🇸 Español](#-español) | [🇺🇸 English](#-english)

---

## 🇪🇸 Español

**WinOpt Studio** es una suite portable de diagnóstico, limpieza y optimización para **Windows 10 y Windows 11**, desarrollada en **C# (.NET Framework)** y **Batch**. Está diseñada para liberar memoria RAM, eliminar archivos residuales, purgar cachés del sistema y reparar componentes dañados comunicándose directamente con el núcleo de Windows, sin necesidad de instalar programas de terceros ni dejar procesos en segundo plano.

---

### 📂 Estructura del Repositorio y Descargas

El repositorio conserva dos versiones independientes organizadas por carpetas dentro de la rama `main`:

| Carpeta | Versión | Descripción | Archivos incluidos | Descarga directa |
| :--- | :--- | :--- | :--- | :--- |
| **[`v4.0-WinOptStudio/`](./v4.0-WinOptStudio)** | **v4.0 (Actual)** | Interfaz gráfica moderna (Dark Mode), modo Simulación (Dry-Run), Turbo Boost de RAM, 7 módulos y exportador de scripts. | `WinOptStudio.exe`<br>`WinOptStudio.cs`<br>`compilar.bat` | **[⬇️ Descargar WinOptStudio.exe](./v4.0-WinOptStudio/WinOptStudio.exe)** |
| **[`v1.0-Consola/`](./v1.0-Consola)** | **v1.0 (Clásica)** | Herramienta ultraligera en consola CMD con menú interactivo y lanzador con auto-elevación de administrador. | `MantenimientoSistema.exe`<br>`MantenimientoSistema.bat`<br>`Lanzador.cs` | **[⬇️ Descargar MantenimientoSistema.exe](./v1.0-Consola/MantenimientoSistema.exe)** |

---

### ⚙️ ¿Cómo funciona por dentro? (Arquitectura Técnica)

A diferencia de los optimizadores tradicionales que dependen de librerías externas pesadas, **WinOpt Studio v4.0** está construido sobre una arquitectura limpia de **3 capas** contenida en código fuente puro (`WinOptStudio.cs`):

1. **Capa de Interfaz (UI / GDI+):**
   * Renderiza controles personalizados con aceleración gráfica (`DoubleBuffered`) y soporte **DPI-Aware (`PerMonitorV2`)** para verse nítida en cualquier resolución o escala de pantalla.
   * Integra el modo oscuro nativo en la barra de título de Windows mediante `dwmapi.dll` (`DwmSetWindowAttribute`).
   * Ejecuta todas las tareas de forma asíncrona (`async/await`), evitando que la ventana se congele mientras analiza o limpia el sistema.

2. **Capa de Motor y Telemetría (Core):**
   * **Simulación / Análisis previo (Dry-Run):** Antes de borrar un solo archivo, el motor recorre las rutas del sistema en **modo solo lectura** calculando el tamaño exacto en bytes (`MB` / `GB`) que se puede recuperar y la memoria RAM liberable, mostrando las métricas en cada tarjeta y en la terminal.
   * **Protección de Integridad:** Incluye una casilla para generar un **Punto de Restauración del Sistema** antes de ejecutar tareas profundas.
   * **Detección inteligente de reinicio:** Si el usuario ejecuta el reseteo de `Winsock`, la aplicación detecta el cambio y muestra un aviso para reiniciar el equipo cuando sea conveniente.

3. **Capa Nativa del Sistema (Windows Internals / P/Invoke):**
   * **`ntdll.dll` (`RtlGetVersion`):** Consulta la compilación exacta del kernel para identificar correctamente **Windows 11** y **Windows 10** sin depender de reportes heredados.
   * **`psapi.dll` (`EmptyWorkingSet`):** Recorre los procesos activos en segundo plano y recorta su conjunto de trabajo (*Working Set*) para devolver memoria física libre al sistema sin cerrar tus programas abiertos.
   * **`kernel32.dll` (`GlobalMemoryStatusEx`):** Mide la memoria RAM física disponible real antes y después de cada optimización para mostrar métricas exactas.
   * **`shell32.dll` (`SHQueryRecycleBin` y `SHEmptyRecycleBin`):** Consulta el peso acumulado en la Papelera de Reciclaje y la vacía de forma silenciosa.
   * **`srclient.dll` (`SRSetRestorePointW`):** Crea puntos de restauración nativos en Windows antes de aplicar cambios críticos.

---

### 🛠️ Detalle de los 7 Módulos de Mantenimiento (v4.0)

| # | Módulo | ¿Qué hace exactamente? | Impacto en el equipo |
| :-: | :--- | :--- | :--- |
| **1** | **Archivos Temporales y Prefetch** | Limpia `%TEMP%`, `C:\Windows\Temp` y `C:\Windows\Prefetch`, omitiendo de forma segura los archivos bloqueados que estén en uso activo. | Libera espacio en disco y elimina basura acumulada. |
| **2** | **Optimización de Memoria RAM** | Invoca `EmptyWorkingSet` sobre los procesos del usuario para liberar memoria en caché retenida innecesariamente. | Reduce el consumo de RAM al instante. |
| **3** | **Red: Caché DNS y Winsock** | Ejecuta `ipconfig /flushdns` y `netsh winsock reset` para purgar resoluciones de dominio antiguas y restablecer el catálogo de red. | Soluciona cortes de conexión y páginas que no cargan. |
| **4** | **Caché de Windows Update** | Detiene temporalmente los servicios `wuauserv` y `bits`, limpia `SoftwareDistribution\Download` y reinicia ambos servicios automáticamente. | Recupera gigabytes de actualizaciones viejas ya instaladas. |
| **5** | **Papelera y Caché Miniaturas** | Vacía la Papelera de Reciclaje y elimina las bases de datos de vistas previas (`thumbcache_*.db`) del Explorador de Windows. | Libera espacio oculto y corrige miniaturas dañadas. |
| **6** | **Reparación de Sistema (DISM / SFC)** | Ejecuta `DISM /Online /Cleanup-Image /RestoreHealth` seguido de `sfc /scannow` para verificar y reparar archivos protegidos de Windows. | Restaura la estabilidad del sistema operativo. |
| **7** | **Shaders GPU y Energía** | Limpia la caché de sombreadores de DirectX (`D3DSCache`), NVIDIA y AMD, y muestra el plan de energía activo (`powercfg`). | Corrige tirones (*stuttering*) gráficos tras actualizar drivers. |

#### ⚡ Funciones Extra en v4.0
* **Botón Turbo Boost:** Ejecuta una liberación inmediata de memoria RAM en menos de un segundo sin tocar el disco duro.
* **Exportador Dinámico de Scripts (`.bat` / `.ps1`):** Genera un archivo automatizado listo para usar que contiene **únicamente** los módulos que tengas activados en pantalla (con codificación UTF-8 `chcp 65001` para conservar acentos).
* **Terminal en Tiempo Real:** Muestra el registro detallado de cada operación con botones para **Copiar** o **Guardar** el historial en texto.

---

### 🚀 Cómo usar o compilar desde el código fuente

#### Opción 1: Ejecución directa (Recomendada)
1. Descarga **`WinOptStudio.exe`** desde la carpeta [`v4.0-WinOptStudio/`](./v4.0-WinOptStudio/WinOptStudio.exe) (o desde la sección **Releases**).
2. Haz doble clic sobre el archivo. El programa solicitará automáticamente permisos de **Administrador (UAC)** para poder acceder a las carpetas protegidas y APIs de memoria de Windows.
3. Pulsa **Simular / Analizar (Dry-Run)** para ver cuánto espacio y RAM puedes recuperar, selecciona los módulos deseados y haz clic en **Ejecutar Optimización**.

#### Opción 2: Compilar el código fuente tú mismo (`compilar.bat`)
No necesitas instalar Visual Studio ni descargar SDKs pesados. Windows incluye de fábrica el compilador oficial de C# (`csc.exe`) dentro de `.NET Framework 4.0+`:
1. Descarga `WinOptStudio.cs` y `compilar.bat` de la carpeta [`v4.0-WinOptStudio/`](./v4.0-WinOptStudio).
2. Coloca ambos archivos en la misma carpeta y haz doble clic en **`compilar.bat`**.
3. El script localizará automáticamente `csc.exe` en `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\` y generará un ejecutable limpio `WinOptStudio.exe` en segundos.

---

### 📟 Sobre la Versión 1.0 (`v1.0-Consola`)
Si prefieres una herramienta minimalista sin interfaz gráfica para equipos antiguos o servidores:
* **`MantenimientoSistema.bat`**: Script estructurado en Batch que verifica permisos de administrador, eleva privilegios vía PowerShell si es necesario y presenta un menú numérico en consola (`1` al `7`).
* **`Lanzador.cs` / `MantenimientoSistema.exe`**: Envoltorio ligero en C# que empaqueta y ejecuta el script de mantenimiento como un binario nativo `.exe`.

---
---

## 🇺🇸 English

**WinOpt Studio** is a portable diagnostics, cleanup, and performance optimization suite for **Windows 10 and Windows 11**, built in **C# (.NET Framework)** and **Batch**. It communicates directly with native Windows APIs to free up RAM, remove residual junk files, purge system caches, and repair corrupted OS components without third-party dependencies or background services.

---

### 📂 Repository Structure & Downloads

Both versions are maintained side-by-side inside the `main` branch:

| Folder | Version | Description | Included Files | Direct Download |
| :--- | :--- | :--- | :--- | :--- |
| **[`v4.0-WinOptStudio/`](./v4.0-WinOptStudio)** | **v4.0 (Latest)** | Modern Dark Mode GUI, Dry-Run Simulation mode, instant RAM Turbo Boost, 7 maintenance modules, and `.bat`/`.ps1` script exporter. | `WinOptStudio.exe`<br>`WinOptStudio.cs`<br>`compilar.bat` | **[⬇️ Download WinOptStudio.exe](./v4.0-WinOptStudio/WinOptStudio.exe)** |
| **[`v1.0-Consola/`](./v1.0-Consola)** | **v1.0 (Classic)** | Lightweight interactive CMD console menu with auto-UAC privilege elevation wrapper. | `MantenimientoSistema.exe`<br>`MantenimientoSistema.bat`<br>`Lanzador.cs` | **[⬇️ Download MantenimientoSistema.exe](./v1.0-Consola/MantenimientoSistema.exe)** |

---

### ⚙️ How It Works Under the Hood

**WinOpt Studio v4.0** is engineered as a self-contained **3-layer architecture** inside `WinOptStudio.cs`:

1. **Presentation Layer (GDI+ & DWM):** Custom double-buffered dark UI with `PerMonitorV2` DPI scaling, native dark title bar integration (`dwmapi.dll` `DwmSetWindowAttribute`), and asynchronous execution (`async/await`) so the interface remains responsive during heavy scans.
2. **Core Engine & Telemetry:**
   * **Dry-Run Simulation Mode:** Scans target directories and memory status in **read-only mode** before deleting anything, displaying recoverable disk space (`MB`/`GB`) and releasable RAM per module.
   * **Safety Controls:** Optional **System Restore Point** creation prior to deep repairs, plus smart reboot prompts when resetting Winsock.
3. **Windows Internals Layer (P/Invoke):**
   * `ntdll.dll` (`RtlGetVersion`): Accurate Windows 10/11 kernel build detection.
   * `psapi.dll` (`EmptyWorkingSet`): Trims background process working sets to reclaim physical RAM without closing active apps.
   * `kernel32.dll` (`GlobalMemoryStatusEx`): Measures real physical memory before and after optimization.
   * `shell32.dll` (`SHQueryRecycleBin` / `SHEmptyRecycleBin`): Queries and empties the Recycle Bin silently.
   * `srclient.dll` (`SRSetRestorePointW`): Creates native Windows System Restore Points.

---

### 🛠️ The 7 Maintenance Modules (v4.0)

1. **Temp Files & Prefetch:** Cleans `%TEMP%`, `C:\Windows\Temp`, and `C:\Windows\Prefetch`, safely skipping locked files currently in use.
2. **RAM Memory Optimization:** Calls `EmptyWorkingSet` across active user processes to release cached physical memory immediately.
3. **Network (DNS & Winsock):** Runs `ipconfig /flushdns` and `netsh winsock reset` to clear stale DNS entries and repair network sockets.
4. **Windows Update Cache:** Safely stops `wuauserv` and `bits`, clears `SoftwareDistribution\Download`, and restarts both services automatically.
5. **Recycle Bin & Thumbnail Cache:** Empties the Recycle Bin and removes Explorer thumbnail databases (`thumbcache_*.db`).
6. **System Repair (DISM / SFC):** Runs `DISM /Online /Cleanup-Image /RestoreHealth` and `sfc /scannow` to fix corrupted system files.
7. **GPU Shader Cache & Power Plan:** Clears DirectX (`D3DSCache`), NVIDIA, and AMD shader caches while reporting the active Windows power scheme.

---

### 🚀 Usage & Self-Compilation

* **Portable Execution:** Download [`WinOptStudio.exe`](./v4.0-WinOptStudio/WinOptStudio.exe) and run it. It automatically requests Administrator privileges (UAC) required for system-level cleanup.
* **Compile from Source (No Visual Studio required):** Place `WinOptStudio.cs` and `compilar.bat` in the same folder and run **`compilar.bat`**. It uses Windows' built-in .NET Framework C# compiler (`csc.exe`) to build `WinOptStudio.exe` in seconds.
