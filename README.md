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
| **1** | **Archivos Temporales y Prefetch** | Lim
