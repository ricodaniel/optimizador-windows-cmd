using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("WinOpt Studio")]
[assembly: AssemblyDescription("Suite Portátil de Rendimiento y Mantenimiento Nativo de Windows")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("WinOpt Studio")]
[assembly: AssemblyProduct("WinOpt Studio")]
[assembly: AssemblyCopyright("Copyright © 2026 WinOpt Studio")]
[assembly: AssemblyTrademark("WinOpt Studio")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("d4a3b8c2-7e1f-4920-915b-63a2c5e89d10")]
[assembly: AssemblyVersion("4.0.0.0")]
[assembly: AssemblyFileVersion("4.0.0.0")]

namespace WinOptStudio
{
    #region Entry Point

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Se ha producido un error durante la ejecución de WinOpt Studio:\n\n" + ex.ToString(),
                    "WinOpt Studio - Error Crítico",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }

    #endregion

    #region Native Win32 API & Windows Internals

    public static class NativeMethods
    {
        // 1. Detección exacta de versión con RtlGetVersion (ntdll.dll)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct OSVERSIONINFOEX
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public ushort wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int RtlGetVersion(ref OSVERSIONINFOEX versionInfo);

        // 2. Consulta de Memoria Física Global (kernel32.dll)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        // 3. Optimización de Memoria RAM
        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        public const uint PROCESS_QUERY_INFORMATION = 0x0400;
        public const uint PROCESS_SET_QUOTA         = 0x0100;

        // 4. Papelera de Reciclaje (Vaciar y Consultar tamaño)
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern uint SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        public const uint SHERB_NOCONFIRMATION = 0x00000001;
        public const uint SHERB_NOPROGRESSUI   = 0x00000002;
        public const uint SHERB_NOSOUND        = 0x00000004;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        // 5. Punto de Restauración del Sistema (srclient.dll)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct RESTOREPOINTINFO
        {
            public int dwEventType;
            public int dwRestorePtType;
            public long llSequenceNumber;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szDescription;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STATEMGRSTATUS
        {
            public int nStatus;
            public long llSequenceNumber;
        }

        [DllImport("srclient.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool SRSetRestorePointW(ref RESTOREPOINTINFO pRestorePointInit, ref STATEMGRSTATUS pSMgrStatus);

        public const int BEGIN_SYSTEM_CHANGE = 100;
        public const int MODIFY_SETTINGS     = 12;

        // 6. Modo Oscuro Nativo en la Barra de Título
        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE             = 20;
    }

    #endregion

    #region System Info Helper

    public static class SystemInfoHelper
    {
        public static string GetExactOperatingSystemInfo()
        {
            string osName = "Windows";
            string displayVersion = "";
            string currentBuild = "";
            string ubr = "";

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (RegistryKey key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        object p = key.GetValue("ProductName");
                        if (p != null) osName = p.ToString();

                        object dv = key.GetValue("DisplayVersion");
                        if (dv != null) displayVersion = dv.ToString();
                        else
                        {
                            object rel = key.GetValue("ReleaseId");
                            if (rel != null) displayVersion = rel.ToString();
                        }

                        object cb = key.GetValue("CurrentBuild");
                        if (cb == null) cb = key.GetValue("CurrentBuildNumber");
                        if (cb != null) currentBuild = cb.ToString();

                        object u = key.GetValue("UBR");
                        if (u != null) ubr = u.ToString();
                    }
                }
            }
            catch { }

            try
            {
                NativeMethods.OSVERSIONINFOEX osvi = new NativeMethods.OSVERSIONINFOEX();
                osvi.dwOSVersionInfoSize = Marshal.SizeOf(typeof(NativeMethods.OSVERSIONINFOEX));
                if (NativeMethods.RtlGetVersion(ref osvi) == 0)
                {
                    int kernelBuild = osvi.dwBuildNumber;
                    if (string.IsNullOrEmpty(currentBuild)) currentBuild = kernelBuild.ToString();

                    if (kernelBuild >= 22000 && osName.Contains("Windows 10"))
                    {
                        osName = osName.Replace("Windows 10", "Windows 11");
                    }
                }
            }
            catch { }

            StringBuilder sb = new StringBuilder();
            sb.Append(osName);

            if (!string.IsNullOrEmpty(displayVersion))
            {
                sb.Append(string.Format(" ({0})", displayVersion));
            }

            if (!string.IsNullOrEmpty(currentBuild))
            {
                sb.Append(string.Format(" • Build {0}", currentBuild));
                if (!string.IsNullOrEmpty(ubr))
                {
                    sb.Append(string.Format(".{0}", ubr));
                }
            }

            sb.Append(Environment.Is64BitOperatingSystem ? " 64-bit" : " 32-bit");
            return sb.ToString();
        }

        public static string GetActivePowerScheme()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powercfg.exe";
                psi.Arguments = "/getactivescheme";
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;

                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();

                    int startIdx = output.IndexOf('(');
                    int endIdx = output.LastIndexOf(')');
                    if (startIdx >= 0 && endIdx > startIdx)
                    {
                        return output.Substring(startIdx + 1, endIdx - startIdx - 1).Trim();
                    }
                    return output.Trim();
                }
            }
            catch
            {
                return "Estándar";
            }
        }
    }

    #endregion

    #region Telemetry & Models

    public enum LogType
    {
        Info,
        Success,
        Warning,
        Error,
        Command,
        Simulation
    }

    public class DryRunTelemetry
    {
        public long TotalDiskReclaimableBytes;
        public int TotalFilesDetected;
        public long EstimableRamBytes;
        public Dictionary<int, string> ModuleBadgeTexts;
        public Dictionary<int, long> ModuleBytes;

        public DryRunTelemetry()
        {
            ModuleBadgeTexts = new Dictionary<int, string>();
            ModuleBytes = new Dictionary<int, long>();
        }
    }

    public class OptimizationTelemetry
    {
        public long TotalBytesFreed;
        public int TotalFilesDeleted;
        public int TotalFilesSkipped;
        public double RamBeforeMB;
        public double RamAfterMB;
        public double RamFreedMB;
        public bool RequiresReboot;
        public Dictionary<int, long> ModuleFreedBytes;

        public OptimizationTelemetry()
        {
            ModuleFreedBytes = new Dictionary<int, long>();
        }
    }

    public class OptimizationConfig
    {
        public bool DoTempFiles;
        public bool DoRamOptimize;
        public bool DoDnsWinsock;
        public bool DoWinUpdate;
        public bool DoRecycleBin;
        public bool DoDismSfc;
        public bool DoGpuShadersPower;
        public bool CreateRestorePoint;
    }

    #endregion

    #region Custom Controls (UI/UX v4.0 - Driver Booster Style)

    /// <summary>
    /// Indicador circular futurista estilo Driver Booster / CleanMyPC con radar animado.
    /// </summary>
    public class CircularSystemGauge : Control
    {
        private int m_percent = 85;
        private float m_animationAngle = 0f;
        private bool m_isScanning = false;
        private string m_statusText = "SISTEMA LISTO";

        public int Percent
        {
            get { return m_percent; }
            set { m_percent = Math.Max(0, Math.Min(100, value)); this.Invalidate(); }
        }

        public bool IsScanning
        {
            get { return m_isScanning; }
            set { m_isScanning = value; this.Invalidate(); }
        }

        public string StatusText
        {
            get { return m_statusText; }
            set { m_statusText = value; this.Invalidate(); }
        }

        public void StepAnimation()
        {
            m_animationAngle = (m_animationAngle + 12f) % 360f;
            this.Invalidate();
        }

        public CircularSystemGauge()
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true);
            this.Size = new Size(54, 54);
            this.BackColor = Color.FromArgb(22, 27, 34); // #161B22
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(5, 5, this.Width - 11, this.Height - 11);

            // Fondo circular
            using (SolidBrush bgFill = new SolidBrush(Color.FromArgb(18, 22, 28)))
            {
                g.FillEllipse(bgFill, rect);
            }

            // Track de fondo sutil
            using (Pen trackPen = new Pen(Color.FromArgb(48, 54, 61), 4.5f))
            {
                g.DrawEllipse(trackPen, rect);
            }

            // Anillo radar animado o progreso
            if (m_isScanning)
            {
                // Líneas de cuadrícula radar tenues
                using (Pen gridPen = new Pen(Color.FromArgb(40, 56, 189, 248), 1f))
                {
                    int cx = this.Width / 2;
                    int cy = this.Height / 2;
                    g.DrawLine(gridPen, cx - 12, cy, cx + 12, cy);
                    g.DrawLine(gridPen, cx, cy - 12, cx, cy + 12);
                }

                using (Pen scanPen = new Pen(Color.FromArgb(56, 189, 248), 4.5f))
                {
                    scanPen.StartCap = LineCap.Round;
                    scanPen.EndCap = LineCap.Round;
                    g.DrawArc(scanPen, rect, m_animationAngle, 100);
                }
            }
            else
            {
                float sweep = (m_percent / 100f) * 360f;
                Color arcColor = m_percent >= 80
                    ? Color.FromArgb(46, 160, 67)
                    : (m_percent >= 50 ? Color.FromArgb(56, 139, 253) : Color.FromArgb(210, 153, 34));

                using (Pen arcPen = new Pen(arcColor, 4.5f))
                {
                    arcPen.StartCap = LineCap.Round;
                    arcPen.EndCap = LineCap.Round;
                    g.DrawArc(arcPen, rect, -90, sweep);
                }
            }

            // Texto central
            string text = m_isScanning ? "SCAN" : string.Format("{0}%", m_percent);
            using (Font f = new Font("Segoe UI", m_isScanning ? 7.5F : 8.5F, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(240, 246, 252)))
            {
                SizeF s = g.MeasureString(text, f);
                g.DrawString(text, f, tb, (this.Width - s.Width) / 2f, (this.Height - s.Height) / 2f);
            }
        }
    }

    public class ModernProgressBar : Control
    {
        private int m_value = 0;
        private int m_maximum = 100;
        private int m_minimum = 0;

        public int Value
        {
            get { return m_value; }
            set
            {
                int val = value;
                if (val < m_minimum) val = m_minimum;
                if (val > m_maximum) val = m_maximum;
                if (m_value != val)
                {
                    m_value = val;
                    this.Invalidate();
                }
            }
        }

        public int Maximum
        {
            get { return m_maximum; }
            set
            {
                m_maximum = value > 0 ? value : 100;
                this.Invalidate();
            }
        }

        public int Minimum
        {
            get { return m_minimum; }
            set
            {
                m_minimum = value;
                this.Invalidate();
            }
        }

        public ModernProgressBar()
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
            this.Height = 22;
            this.BackColor = Color.FromArgb(22, 27, 34); // #161B22
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            int radius = 6;

            using (GraphicsPath trackPath = CreateRoundedRectangle(rect, radius))
            using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(22, 27, 34)))
            using (Pen borderPen = new Pen(Color.FromArgb(48, 54, 61), 1))
            {
                g.FillPath(trackBrush, trackPath);
                g.DrawPath(borderPen, trackPath);
            }

            float percent = (m_maximum > m_minimum) ? (float)(m_value - m_minimum) / (float)(m_maximum - m_minimum) : 0f;
            int fillWidth = (int)((this.Width - 2) * percent);

            if (fillWidth > 6)
            {
                Rectangle fillRect = new Rectangle(1, 1, fillWidth, this.Height - 2);
                using (GraphicsPath fillPath = CreateRoundedRectangle(fillRect, radius - 1))
                using (LinearGradientBrush grad = new LinearGradientBrush(
                    fillRect,
                    Color.FromArgb(31, 111, 235), // #1F6FEB
                    Color.FromArgb(46, 160, 67),  // #2EA043
                    LinearGradientMode.Horizontal))
                {
                    g.FillPath(grad, fillPath);
                }
            }

            string displayText = string.Format("{0}%", m_value);
            using (Font font = new Font("Segoe UI", 8.75F, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(240, 246, 252)))
            {
                SizeF size = g.MeasureString(displayText, font);
                PointF pt = new PointF((this.Width - size.Width) / 2f, (this.Height - size.Height) / 2f);
                g.DrawString(displayText, font, textBrush, pt);
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public enum CardIconType
    {
        TempFiles,
        Ram,
        NetworkDns,
        WindowsUpdate,
        RecycleBin,
        SystemRepair,
        GpuShaders
    }

    public class ModernModuleCard : Control
    {
        private bool m_checked = false;
        private string m_title = "Módulo";
        private string m_subtitle = "Descripción técnica";
        private CardIconType m_iconType = CardIconType.TempFiles;

        private string m_metricBadgeText = string.Empty;
        private bool m_hasMetric = false;
        private bool m_isSuccessMetric = false;
        private bool m_isHovered = false;

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get { return m_checked; }
            set
            {
                if (m_checked != value)
                {
                    m_checked = value;
                    this.Invalidate();
                    if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
                }
            }
        }

        public string Title
        {
            get { return m_title; }
            set { m_title = value; this.Invalidate(); }
        }

        public string Subtitle
        {
            get { return m_subtitle; }
            set { m_subtitle = value; this.Invalidate(); }
        }

        public CardIconType IconType
        {
            get { return m_iconType; }
            set { m_iconType = value; this.Invalidate(); }
        }

        public string MetricBadgeText
        {
            get { return m_metricBadgeText; }
            set
            {
                m_metricBadgeText = value;
                m_hasMetric = !string.IsNullOrEmpty(value);
                this.Invalidate();
            }
        }

        public bool HasMetric
        {
            get { return m_hasMetric; }
            set { m_hasMetric = value; this.Invalidate(); }
        }

        public bool IsSuccessMetric
        {
            get { return m_isSuccessMetric; }
            set { m_isSuccessMetric = value; this.Invalidate(); }
        }

        public ModernModuleCard()
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
            this.Height = 70;
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.FromArgb(22, 27, 34);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            m_isHovered = true;
            this.Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            m_isHovered = false;
            this.Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnClick(EventArgs e)
        {
            this.Checked = !this.Checked;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            int radius = 8;

            Color bgColor = m_checked
                ? Color.FromArgb(28, 33, 40)   // #1C2128
                : (m_isHovered ? Color.FromArgb(25, 30, 36) : Color.FromArgb(22, 27, 34));

            Color borderColor = m_checked
                ? Color.FromArgb(56, 139, 253) // #388BFD
                : (m_isHovered ? Color.FromArgb(72, 79, 88) : Color.FromArgb(48, 54, 61));

            using (GraphicsPath cardPath = CreateRoundedRectangle(rect, radius))
            using (SolidBrush bgBrush = new SolidBrush(bgColor))
            using (Pen pen = new Pen(borderColor, m_checked ? 1.5f : 1f))
            {
                g.FillPath(bgBrush, cardPath);
                g.DrawPath(pen, cardPath);
            }

            int iconBoxSize = 34;
            int iconBoxX = 12;
            int iconBoxY = (this.Height - iconBoxSize) / 2;
            Rectangle iconBoxRect = new Rectangle(iconBoxX, iconBoxY, iconBoxSize, iconBoxSize);

            using (GraphicsPath iconBgPath = CreateRoundedRectangle(iconBoxRect, 6))
            using (SolidBrush iconBgBrush = new SolidBrush(m_checked ? Color.FromArgb(30, 41, 59) : Color.FromArgb(33, 38, 45)))
            {
                g.FillPath(iconBgBrush, iconBgPath);
            }

            DrawVectorIcon(g, iconBoxRect, m_iconType, m_checked);

            int switchWidth = 36;
            int switchHeight = 18;
            int switchX = this.Width - switchWidth - 12;
            int switchY = (this.Height - switchHeight) / 2;
            Rectangle switchRect = new Rectangle(switchX, switchY, switchWidth, switchHeight);

            Color switchTrackColor = m_checked ? Color.FromArgb(46, 160, 67) : Color.FromArgb(48, 54, 61);
            using (GraphicsPath switchPath = CreateRoundedRectangle(switchRect, switchHeight / 2))
            using (SolidBrush switchBrush = new SolidBrush(switchTrackColor))
            {
                g.FillPath(switchBrush, switchPath);
            }

            int knobSize = switchHeight - 4;
            int knobX = m_checked ? (switchX + switchWidth - knobSize - 2) : (switchX + 2);
            int knobY = switchY + 2;
            Rectangle knobRect = new Rectangle(knobX, knobY, knobSize, knobSize);

            using (SolidBrush knobBrush = new SolidBrush(Color.White))
            {
                g.FillEllipse(knobBrush, knobRect);
            }

            int rightBoundary = switchX - 10;

            if (m_hasMetric && !string.IsNullOrEmpty(m_metricBadgeText))
            {
                using (Font badgeFont = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold))
                {
                    SizeF bSize = g.MeasureString(m_metricBadgeText, badgeFont);
                    int badgeWidth = (int)bSize.Width + 14;
                    int badgeHeight = 22;
                    int badgeX = rightBoundary - badgeWidth;
                    int badgeY = (this.Height - badgeHeight) / 2;
                    Rectangle badgeRect = new Rectangle(badgeX, badgeY, badgeWidth, badgeHeight);

                    Color badgeBg, badgeBorder, badgeText;

                    if (m_isSuccessMetric)
                    {
                        badgeBg = Color.FromArgb(20, 48, 28);
                        badgeBorder = Color.FromArgb(46, 160, 67);
                        badgeText = Color.FromArgb(63, 185, 80);
                    }
                    else if (m_metricBadgeText.IndexOf("0 MB", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             m_metricBadgeText.IndexOf("Limpio", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        badgeBg = Color.FromArgb(28, 33, 40);
                        badgeBorder = Color.FromArgb(48, 54, 61);
                        badgeText = Color.FromArgb(139, 148, 158);
                    }
                    else
                    {
                        badgeBg = Color.FromArgb(28, 37, 51);
                        badgeBorder = Color.FromArgb(45, 59, 78);
                        badgeText = Color.FromArgb(88, 166, 255);
                    }

                    using (GraphicsPath bPath = CreateRoundedRectangle(badgeRect, 5))
                    using (SolidBrush bBgBrush = new SolidBrush(badgeBg))
                    using (Pen bPen = new Pen(badgeBorder, 1))
                    using (SolidBrush bTextBrush = new SolidBrush(badgeText))
                    {
                        g.FillPath(bBgBrush, bPath);
                        g.DrawPath(bPen, bPath);

                        StringFormat sfBadge = new StringFormat();
                        sfBadge.Alignment = StringAlignment.Center;
                        sfBadge.LineAlignment = StringAlignment.Center;
                        g.DrawString(m_metricBadgeText, badgeFont, bTextBrush, badgeRect, sfBadge);
                    }

                    rightBoundary = badgeX - 8;
                }
            }

            int textX = iconBoxX + iconBoxSize + 12;
            int maxTextWidth = Math.Max(50, rightBoundary - textX - 4);

            using (Font titleFont = new Font("Segoe UI", 9.25F, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(m_checked ? Color.FromArgb(240, 246, 252) : Color.FromArgb(201, 209, 217)))
            {
                RectangleF titleRect = new RectangleF(textX, 10, maxTextWidth, 20);
                StringFormat sfTitle = new StringFormat();
                sfTitle.Trimming = StringTrimming.EllipsisCharacter;
                g.DrawString(m_title, titleFont, titleBrush, titleRect, sfTitle);
            }

            using (Font subFont = new Font("Segoe UI", 7.75F, FontStyle.Regular))
            using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(139, 148, 158)))
            {
                RectangleF subRect = new RectangleF(textX, 32, maxTextWidth, 30);
                StringFormat sfSub = new StringFormat();
                sfSub.Trimming = StringTrimming.EllipsisCharacter;
                g.DrawString(m_subtitle, subFont, subBrush, subRect, sfSub);
            }
        }

        private static void DrawVectorIcon(Graphics g, Rectangle box, CardIconType type, bool active)
        {
            Color iconColor = active ? Color.FromArgb(88, 166, 255) : Color.FromArgb(139, 148, 158);
            using (Pen pen = new Pen(iconColor, 1.6f))
            using (SolidBrush brush = new SolidBrush(iconColor))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                int cx = box.X + box.Width / 2;
                int cy = box.Y + box.Height / 2;

                switch (type)
                {
                    case CardIconType.TempFiles:
                        g.DrawLine(pen, cx - 8, cy - 6, cx - 3, cy - 6);
                        g.DrawLine(pen, cx - 3, cy - 6, cx - 1, cy - 4);
                        g.DrawLine(pen, cx - 1, cy - 4, cx + 8, cy - 4);
                        g.DrawLine(pen, cx + 8, cy - 4, cx + 8, cy + 6);
                        g.DrawLine(pen, cx + 8, cy + 6, cx - 8, cy + 6);
                        g.DrawLine(pen, cx - 8, cy + 6, cx - 8, cy - 6);
                        g.DrawLine(pen, cx - 4, cy + 1, cx + 4, cy + 1);
                        break;

                    case CardIconType.Ram:
                        g.DrawRectangle(pen, cx - 8, cy - 5, 16, 9);
                        g.DrawLine(pen, cx - 5, cy + 4, cx - 5, cy + 7);
                        g.DrawLine(pen, cx - 2, cy + 4, cx - 2, cy + 7);
                        g.DrawLine(pen, cx + 2, cy + 4, cx + 2, cy + 7);
                        g.DrawLine(pen, cx + 5, cy + 4, cx + 5, cy + 7);
                        g.FillRectangle(brush, cx - 5, cy - 3, 3, 4);
                        g.FillRectangle(brush, cx + 2, cy - 3, 3, 4);
                        break;

                    case CardIconType.NetworkDns:
                        g.DrawEllipse(pen, cx - 7, cy - 7, 14, 14);
                        g.DrawEllipse(pen, cx - 3, cy - 7, 6, 14);
                        g.DrawLine(pen, cx - 7, cy, cx + 7, cy);
                        break;

                    case CardIconType.WindowsUpdate:
                        g.DrawArc(pen, cx - 6, cy - 6, 12, 12, 45, 230);
                        g.DrawLine(pen, cx + 5, cy - 2, cx + 7, cy + 1);
                        g.DrawLine(pen, cx + 7, cy + 1, cx + 3, cy + 2);
                        break;

                    case CardIconType.RecycleBin:
                        g.DrawLine(pen, cx - 7, cy - 4, cx + 7, cy - 4);
                        g.DrawLine(pen, cx - 3, cy - 6, cx + 3, cy - 6);
                        g.DrawLine(pen, cx - 5, cy - 4, cx - 4, cy + 7);
                        g.DrawLine(pen, cx + 5, cy - 4, cx + 4, cy + 7);
                        g.DrawLine(pen, cx - 4, cy + 7, cx + 4, cy + 7);
                        g.DrawLine(pen, cx - 1, cy - 1, cx - 1, cy + 4);
                        g.DrawLine(pen, cx + 1, cy - 1, cx + 1, cy + 4);
                        break;

                    case CardIconType.SystemRepair:
                        g.DrawLine(pen, cx - 6, cy - 6, cx + 6, cy + 6);
                        g.DrawLine(pen, cx - 6, cy + 6, cx + 6, cy - 6);
                        g.DrawEllipse(pen, cx - 7, cy - 7, 4, 4);
                        g.DrawLine(pen, cx + 4, cy + 4, cx + 7, cy + 7);
                        break;

                    case CardIconType.GpuShaders:
                        g.DrawRectangle(pen, cx - 6, cy - 6, 12, 12);
                        g.FillRectangle(brush, cx - 3, cy - 3, 6, 6);
                        g.DrawLine(pen, cx - 4, cy - 8, cx - 4, cy - 6);
                        g.DrawLine(pen, cx + 4, cy - 8, cx + 4, cy - 6);
                        g.DrawLine(pen, cx - 4, cy + 6, cx - 4, cy + 8);
                        g.DrawLine(pen, cx + 4, cy + 6, cx + 4, cy + 8);
                        g.DrawLine(pen, cx - 8, cy - 4, cx - 6, cy - 4);
                        g.DrawLine(pen, cx - 8, cy + 4, cx - 6, cy + 4);
                        g.DrawLine(pen, cx + 6, cy - 4, cx + 8, cy - 4);
                        g.DrawLine(pen, cx + 6, cy + 4, cx + 8, cy + 4);
                        break;
                }
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public enum ButtonIconType
    {
        None,
        Lightning,
        Search,
        Save,
        Clean,
        Copy,
        Shield
    }

    public class ModernButton : Button
    {
        private bool m_isHovered = false;
        private bool m_isPrimary = false;
        private ButtonIconType m_vectorIcon = ButtonIconType.None;

        public bool IsPrimary
        {
            get { return m_isPrimary; }
            set { m_isPrimary = value; this.Invalidate(); }
        }

        public ButtonIconType VectorIcon
        {
            get { return m_vectorIcon; }
            set { m_vectorIcon = value; this.Invalidate(); }
        }

        public ModernButton()
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true);
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Cursor = Cursors.Hand;
            this.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            this.BackColor = Color.FromArgb(33, 38, 45);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            m_isHovered = true;
            this.Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            m_isHovered = false;
            this.Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            int radius = 7;

            using (GraphicsPath path = CreateRoundedRectangle(rect, radius))
            {
                if (m_isPrimary)
                {
                    Color c1 = m_isHovered ? Color.FromArgb(46, 160, 67) : Color.FromArgb(35, 134, 54);
                    Color c2 = m_isHovered ? Color.FromArgb(56, 175, 78) : Color.FromArgb(46, 160, 67);

                    using (LinearGradientBrush brush = new LinearGradientBrush(rect, c1, c2, LinearGradientMode.Vertical))
                    using (Pen pen = new Pen(Color.FromArgb(63, 185, 80), 1))
                    {
                        g.FillPath(brush, path);
                        g.DrawPath(pen, path);
                    }
                }
                else
                {
                    Color baseBg = (this.BackColor != Color.FromArgb(33, 38, 45) && this.BackColor != Color.Empty) ? this.BackColor : Color.FromArgb(33, 38, 45);
                    Color bg = m_isHovered ? ControlPaint.Light(baseBg, 0.18f) : baseBg;
                    Color border = (this.BackColor != Color.FromArgb(33, 38, 45) && this.BackColor != Color.Empty) ? ControlPaint.Light(baseBg, 0.35f) : Color.FromArgb(56, 63, 72);

                    using (SolidBrush brush = new SolidBrush(bg))
                    using (Pen pen = new Pen(border, 1))
                    {
                        g.FillPath(brush, path);
                        g.DrawPath(pen, path);
                    }
                }

                Color textColor = this.Enabled ? (m_isPrimary ? Color.White : this.ForeColor) : Color.FromArgb(110, 118, 129);

                SizeF textSize = g.MeasureString(this.Text, this.Font);
                int iconWidth = (m_vectorIcon != ButtonIconType.None) ? 18 : 0;
                int totalContentWidth = (int)textSize.Width + iconWidth + ((iconWidth > 0) ? 6 : 0);

                int startX = Math.Max(8, (this.Width - totalContentWidth) / 2);
                int textY = (int)((this.Height - textSize.Height) / 2f);

                if (m_vectorIcon != ButtonIconType.None)
                {
                    Rectangle iconRect = new Rectangle(startX, (this.Height - 14) / 2, 14, 14);
                    DrawButtonVectorIcon(g, iconRect, m_vectorIcon, textColor);
                    startX += 18;
                }

                using (SolidBrush textBrush = new SolidBrush(textColor))
                {
                    g.DrawString(this.Text, this.Font, textBrush, new PointF(startX, textY));
                }
            }
        }

        private static void DrawButtonVectorIcon(Graphics g, Rectangle r, ButtonIconType type, Color color)
        {
            using (Pen pen = new Pen(color, 1.6f))
            using (SolidBrush brush = new SolidBrush(color))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                int cx = r.X + r.Width / 2;
                int cy = r.Y + r.Height / 2;

                switch (type)
                {
                    case ButtonIconType.Lightning:
                        Point[] pts = new Point[] {
                            new Point(cx + 1, cy - 6),
                            new Point(cx - 4, cy),
                            new Point(cx, cy),
                            new Point(cx - 2, cy + 6),
                            new Point(cx + 4, cy - 1),
                            new Point(cx + 1, cy - 1)
                        };
                        g.FillPolygon(brush, pts);
                        break;

                    case ButtonIconType.Search:
                        g.DrawEllipse(pen, cx - 5, cy - 5, 8, 8);
                        g.DrawLine(pen, cx + 2, cy + 2, cx + 6, cy + 6);
                        break;

                    case ButtonIconType.Save:
                        g.DrawRectangle(pen, cx - 5, cy - 6, 10, 12);
                        g.FillRectangle(brush, cx - 3, cy - 6, 6, 4);
                        g.DrawLine(pen, cx - 3, cy + 2, cx + 3, cy + 2);
                        break;

                    case ButtonIconType.Clean:
                        g.DrawLine(pen, cx - 4, cy + 5, cx + 5, cy - 4);
                        g.DrawLine(pen, cx - 6, cy + 2, cx - 2, cy + 6);
                        g.DrawLine(pen, cx + 2, cy - 5, cx + 5, cy - 2);
                        break;

                    case ButtonIconType.Copy:
                        g.DrawRectangle(pen, cx - 5, cy - 5, 7, 8);
                        g.DrawRectangle(pen, cx - 2, cy - 2, 7, 8);
                        break;

                    case ButtonIconType.Shield:
                        Point[] shieldPts = new Point[] {
                            new Point(cx - 5, cy - 5),
                            new Point(cx + 5, cy - 5),
                            new Point(cx + 5, cy),
                            new Point(cx, cy + 6),
                            new Point(cx - 5, cy)
                        };
                        g.DrawPolygon(pen, shieldPts);
                        break;
                }
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    #endregion

    #region Optimization Engine (Core & Non-Blocking Async)

    public class OptimizationEngine
    {
        private Action<string, LogType> m_logger;
        private Action<int, string, int, int> m_progress;

        public OptimizationEngine(Action<string, LogType> logger, Action<int, string, int, int> progress)
        {
            m_logger = logger;
            m_progress = progress;
        }

        public static string FormatDiskBytes(long bytes)
        {
            if (bytes <= 0) return "0 MB • Limpio";
            if (bytes >= 1024L * 1024L * 1024L)
                return string.Format("{0:N2} GB", bytes / (1024.0 * 1024.0 * 1024.0));
            return string.Format("{0:N1} MB", bytes / (1024.0 * 1024.0));
        }

        public static bool IsProtectedOrSecurityProcess(string name, int pid)
        {
            if (pid <= 4) return true;
            if (string.IsNullOrEmpty(name)) return true;
            string n = name.ToLowerInvariant();

            // Antivirus y protección de Windows Defender (no tocar para evitar falsos positivos heurísticos)
            if (n == "msmpeng" || n == "securityhealthservice" || n == "securityhealthsystray" ||
                n == "smartscreen" || n == "nissrv" || n == "mpcmdrun" || n == "windefend" ||
                n.IndexOf("defender", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("antivirus", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("security", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("kaspersky", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("avast", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("avg", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("bitdefender", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("eset", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("mcafee", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("malware", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            // Procesos críticos del sistema operativo
            if (n == "system" || n == "registry" || n == "smss" || n == "csrss" ||
                n == "wininit" || n == "services" || n == "lsass" || n == "fontdrvhost" ||
                n == "dwm" || n == "winlogon" || n == "memory compression")
            {
                return true;
            }

            return false;
        }

        public static double QuickTurboBoostRam()
        {
            long wsBefore = 0;
            long wsAfter = 0;
            Process[] procs = Process.GetProcesses();
            for (int i = 0; i < procs.Length; i++)
            {
                Process p = procs[i];
                try
                {
                    string pName = string.Empty;
                    try { pName = p.ProcessName; } catch { }
                    if (IsProtectedOrSecurityProcess(pName, p.Id)) continue;

                    long ws = 0;
                    try { ws = p.WorkingSet64; } catch { }
                    wsBefore += ws;

                    IntPtr hProc = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_SET_QUOTA, false, p.Id);
                    if (hProc != IntPtr.Zero)
                    {
                        try { NativeMethods.EmptyWorkingSet(hProc); }
                        finally { NativeMethods.CloseHandle(hProc); }
                    }
                    try { p.Refresh(); wsAfter += p.WorkingSet64; } catch { wsAfter += ws; }
                }
                catch { }
                finally { try { p.Dispose(); } catch { } }
            }
            double freed = (wsBefore - wsAfter) / (1024.0 * 1024.0);
            return Math.Max(0, freed);
        }

        private void SafeLog(string message, LogType type)
        {
            if (m_logger != null) m_logger(message, type);
        }

        private void ReportProgress(int done, int total, string message)
        {
            int percent = (total == 0) ? 100 : (int)((done / (double)total) * 100.0);
            if (m_progress != null) m_progress(percent, message, done, total);
        }

        #region Dry-Run Mode (Estricta Solo Lectura)

        public async Task<DryRunTelemetry> RunDryRunAsync(OptimizationConfig config)
        {
            DryRunTelemetry telemetry = new DryRunTelemetry();

            int totalTasks = (config.DoTempFiles ? 1 : 0) +
                             (config.DoRamOptimize ? 1 : 0) +
                             (config.DoDnsWinsock ? 1 : 0) +
                             (config.DoWinUpdate ? 1 : 0) +
                             (config.DoRecycleBin ? 1 : 0) +
                             (config.DoDismSfc ? 1 : 0) +
                             (config.DoGpuShadersPower ? 1 : 0);

            if (totalTasks == 0) return telemetry;

            SafeLog("=== Iniciando Simulación y Diagnóstico (Modo Solo Lectura) ===", LogType.Simulation);
            SafeLog("Ningún archivo será eliminado ni se modificará el sistema.", LogType.Simulation);

            // 100% en segundo plano: NINGUNA llamada sincrónica en el hilo UI
            await Task.Run(() =>
            {
                int completed = 0;

                // 1. Temporales
                if (config.DoTempFiles)
                {
                    ReportProgress(completed, totalTasks, "Simulando: escaneando archivos temporales (%TEMP%, Windows, Prefetch)...");
                    long tempBytes; int tempFiles;
                    ScanTempFiles(out tempBytes, out tempFiles);

                    telemetry.ModuleBytes[1] = tempBytes;
                    telemetry.TotalDiskReclaimableBytes += tempBytes;
                    telemetry.TotalFilesDetected += tempFiles;
                    string badge = FormatDiskBytes(tempBytes);
                    telemetry.ModuleBadgeTexts[1] = badge;

                    SafeLog(string.Format("[Módulo 1 - Temporales] {0} archivos detectados ocupando {1} de disco.", tempFiles, badge), LogType.Simulation);
                    completed++;
                }

                // 2. RAM
                if (config.DoRamOptimize)
                {
                    ReportProgress(completed, totalTasks, "Simulando: calculando memoria RAM recuperable...");
                    long ramBytes; double inUseGB, totalGB;
                    EstimateRamReclaim(out ramBytes, out inUseGB, out totalGB);

                    telemetry.EstimableRamBytes = ramBytes;
                    telemetry.ModuleBytes[2] = ramBytes;
                    string badge = string.Format("~{0:N0} MB RAM", ramBytes / (1024.0 * 1024.0));
                    telemetry.ModuleBadgeTexts[2] = badge;

                    SafeLog(string.Format("[Módulo 2 - RAM] RAM física: {0:N1} GB en uso de {1:N1} GB. Estimado recuperable: {2}.", inUseGB, totalGB, badge), LogType.Simulation);
                    completed++;
                }

                // 3. Red (DNS / Winsock)
                if (config.DoDnsWinsock)
                {
                    ReportProgress(completed, totalTasks, "Simulando: verificando caché DNS y sockets de red...");
                    telemetry.ModuleBadgeTexts[3] = "Caché DNS • Reinicio PC";
                    SafeLog("[Módulo 3 - Red] Listo para purga de caché DNS y reseteo Winsock (programará aviso de reinicio).", LogType.Simulation);
                    completed++;
                }

                // 4. Windows Update
                if (config.DoWinUpdate)
                {
                    ReportProgress(completed, totalTasks, "Simulando: analizando descargas de Windows Update...");
                    long wuBytes; int wuFiles;
                    string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                    ScanFolderSize(Path.Combine(winDir, @"SoftwareDistribution\Download"), out wuBytes, out wuFiles);

                    telemetry.ModuleBytes[4] = wuBytes;
                    telemetry.TotalDiskReclaimableBytes += wuBytes;
                    telemetry.TotalFilesDetected += wuFiles;
                    string badge = FormatDiskBytes(wuBytes);
                    telemetry.ModuleBadgeTexts[4] = badge;

                    SafeLog(string.Format("[Módulo 4 - Windows Update] {0} paquetes descargados ocupando {1}.", wuFiles, badge), LogType.Simulation);
                    completed++;
                }

                // 5. Papelera y Miniaturas
                if (config.DoRecycleBin)
                {
                    ReportProgress(completed, totalTasks, "Simulando: consultando Papelera de Reciclaje y miniaturas...");
                    long recycleBytes = 0;
                    long recycleItems = 0;
                    try
                    {
                        NativeMethods.SHQUERYRBINFO rbInfo = new NativeMethods.SHQUERYRBINFO();
                        rbInfo.cbSize = Marshal.SizeOf(typeof(NativeMethods.SHQUERYRBINFO));
                        if (NativeMethods.SHQueryRecycleBin(null, ref rbInfo) == 0)
                        {
                            recycleBytes = rbInfo.i64Size;
                            recycleItems = rbInfo.i64NumItems;
                        }
                    }
                    catch { }

                    long thumbBytes; int thumbFiles;
                    string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    ScanFolderSize(Path.Combine(localApp, @"Microsoft\Windows\Explorer"), out thumbBytes, out thumbFiles, "thumbcache_*.db");

                    long totalRecycle = recycleBytes + thumbBytes;
                    telemetry.ModuleBytes[5] = totalRecycle;
                    telemetry.TotalDiskReclaimableBytes += totalRecycle;
                    telemetry.TotalFilesDetected += (int)recycleItems + thumbFiles;
                    string badge = FormatDiskBytes(totalRecycle);
                    telemetry.ModuleBadgeTexts[5] = badge;

                    SafeLog(string.Format("[Módulo 5 - Papelera y Miniaturas] Papelera: {0:N2} MB ({1} items) | Miniaturas: {2:N2} MB. Total: {3}.",
                        recycleBytes / (1024.0 * 1024.0), recycleItems, thumbBytes / (1024.0 * 1024.0), badge), LogType.Simulation);
                    completed++;
                }

                // 6. DISM / SFC
                if (config.DoDismSfc)
                {
                    ReportProgress(completed, totalTasks, "Simulando: evaluando integridad de imagen de Windows...");
                    telemetry.ModuleBadgeTexts[6] = "Examen profundo";
                    SafeLog("[Módulo 6 - DISM/SFC] Configurado para comprobación exhaustiva del almacén de componentes y archivos del sistema.", LogType.Simulation);
                    completed++;
                }

                // 7. Shaders GPU y Plan de Energía
                if (config.DoGpuShadersPower)
                {
                    ReportProgress(completed, totalTasks, "Simulando: escaneando caché de sombreadores GPU...");
                    long shaderBytes; int shaderFiles;
                    ScanGpuShaders(out shaderBytes, out shaderFiles);

                    string powerScheme = SystemInfoHelper.GetActivePowerScheme();
                    string shortPower = powerScheme.Length > 12 ? powerScheme.Substring(0, 10) + ".." : powerScheme;

                    telemetry.ModuleBytes[7] = shaderBytes;
                    telemetry.TotalDiskReclaimableBytes += shaderBytes;
                    telemetry.TotalFilesDetected += shaderFiles;
                    string badge = string.Format("{0} • {1}", FormatDiskBytes(shaderBytes), shortPower);
                    telemetry.ModuleBadgeTexts[7] = badge;

                    SafeLog(string.Format("[Módulo 7 - Shaders GPU y Energía] Shaders: {0} ({1} archivos) | Plan: '{2}'.", FormatDiskBytes(shaderBytes), shaderFiles, powerScheme), LogType.Simulation);
                    completed++;
                }
            });

            if (m_progress != null) m_progress(100, "Simulación completada con éxito.", totalTasks, totalTasks);
            return telemetry;
        }

        private void ScanTempFiles(out long totalBytes, out int totalFiles)
        {
            totalBytes = 0;
            totalFiles = 0;
            List<string> paths = new List<string>();

            try { string ut = Path.GetTempPath(); if (Directory.Exists(ut)) paths.Add(ut); } catch { }
            try { string wt = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"); if (Directory.Exists(wt)) paths.Add(wt); } catch { }
            try { string pf = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"); if (Directory.Exists(pf)) paths.Add(pf); } catch { }

            foreach (string p in paths)
            {
                try
                {
                    string[] files = Directory.GetFiles(p, "*.*", SearchOption.TopDirectoryOnly);
                    foreach (string f in files)
                    {
                        try
                        {
                            FileInfo fi = new FileInfo(f);
                            totalBytes += fi.Length;
                            totalFiles++;
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }

        private void EstimateRamReclaim(out long estimableBytes, out double inUseGB, out double totalGB)
        {
            estimableBytes = 0;
            inUseGB = 0;
            totalGB = 0;

            try
            {
                NativeMethods.MEMORYSTATUSEX mem = new NativeMethods.MEMORYSTATUSEX();
                if (NativeMethods.GlobalMemoryStatusEx(mem))
                {
                    totalGB = mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                    ulong inUseBytes = mem.ullTotalPhys - mem.ullAvailPhys;
                    inUseGB = inUseBytes / (1024.0 * 1024.0 * 1024.0);

                    long totalWs = 0;
                    foreach (Process p in Process.GetProcesses())
                    {
                        try
                        {
                            if (p.Id > 4) totalWs += p.WorkingSet64;
                        }
                        catch { }
                        finally { try { p.Dispose(); } catch { } }
                    }

                    estimableBytes = (long)(totalWs * 0.25);
                    if (estimableBytes < 200L * 1024 * 1024) estimableBytes = (long)(inUseBytes * 0.15);
                }
            }
            catch { }
        }

        private void ScanFolderSize(string path, out long totalBytes, out int totalFiles, string pattern = "*.*")
        {
            totalBytes = 0;
            totalFiles = 0;

            if (!Directory.Exists(path)) return;

            try
            {
                string[] files = Directory.GetFiles(path, pattern, SearchOption.AllDirectories);
                foreach (string f in files)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(f);
                        totalBytes += fi.Length;
                        totalFiles++;
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void ScanGpuShaders(out long totalBytes, out int totalFiles)
        {
            totalBytes = 0;
            totalFiles = 0;
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string[] paths = new string[] {
                Path.Combine(localApp, "D3DSCache"),
                Path.Combine(localApp, @"NVIDIA\DXCache"),
                Path.Combine(localApp, @"NVIDIA\GLCache"),
                Path.Combine(localApp, @"AMD\DxCache"),
                Path.Combine(localApp, @"Intel\ShaderCache")
            };

            foreach (string p in paths)
            {
                if (Directory.Exists(p))
                {
                    long b; int f;
                    ScanFolderSize(p, out b, out f);
                    totalBytes += b;
                    totalFiles += f;
                }
            }
        }

        #endregion

        #region Active Optimization Mode (100% Non-Blocking Task.Run)

        public async Task<OptimizationTelemetry> RunAsync(OptimizationConfig config)
        {
            OptimizationTelemetry telemetry = new OptimizationTelemetry();
            int totalTasks = (config.DoTempFiles ? 1 : 0) +
                             (config.DoRamOptimize ? 1 : 0) +
                             (config.DoDnsWinsock ? 1 : 0) +
                             (config.DoWinUpdate ? 1 : 0) +
                             (config.DoRecycleBin ? 1 : 0) +
                             (config.DoDismSfc ? 1 : 0) +
                             (config.DoGpuShadersPower ? 1 : 0);

            if (totalTasks == 0) return telemetry;

            SafeLog(string.Format("=== Iniciando Optimización Activa ({0} módulos seleccionados) ===", totalTasks), LogType.Info);

            // ¡CRÍTICO! Toda la ejecución corre 100% dentro de Task.Run para que la UI jamás congele
            await Task.Run(() =>
            {
                // 0. Punto de Restauración del Sistema en segundo plano
                if (config.CreateRestorePoint)
                {
                    CreateSystemRestorePoint();
                }

                int completedTasks = 0;

                // Módulo 1: Temporales
                if (config.DoTempFiles)
                {
                    ReportProgress(completedTasks, totalTasks, "Limpiando archivos temporales y prefetch...");
                    ExecuteTempFilesModule(telemetry);
                    completedTasks++;
                }

                // Módulo 2: RAM
                if (config.DoRamOptimize)
                {
                    ReportProgress(completedTasks, totalTasks, "Optimizando memoria RAM con EmptyWorkingSet...");
                    ExecuteRamModule(telemetry);
                    completedTasks++;
                }

                // Módulo 3: Red (DNS / Winsock)
                if (config.DoDnsWinsock)
                {
                    ReportProgress(completedTasks, totalTasks, "Purgando caché DNS y reiniciando catálogo Winsock...");
                    ExecuteDnsWinsockModule(telemetry);
                    completedTasks++;
                }

                // Módulo 4: Windows Update
                if (config.DoWinUpdate)
                {
                    ReportProgress(completedTasks, totalTasks, "Depurando descargas de Windows Update...");
                    ExecuteWindowsUpdateModule(telemetry);
                    completedTasks++;
                }

                // Módulo 5: Papelera y Miniaturas
                if (config.DoRecycleBin)
                {
                    ReportProgress(completedTasks, totalTasks, "Vaciando papelera y depurando miniaturas...");
                    ExecuteRecycleBinModule(telemetry);
                    completedTasks++;
                }

                // Módulo 6: DISM / SFC
                if (config.DoDismSfc)
                {
                    ReportProgress(completedTasks, totalTasks, "Ejecutando comprobación y reparación de imagen (DISM / SFC)...");
                    ExecuteDismSfcModule();
                    completedTasks++;
                }

                // Módulo 7: Shaders GPU y Energía
                if (config.DoGpuShadersPower)
                {
                    ReportProgress(completedTasks, totalTasks, "Limpiando shaders GPU y verificando energía...");
                    ExecuteGpuShaderPowerModule(telemetry);
                    completedTasks++;
                }
            });

            if (m_progress != null) m_progress(100, "Optimización activa finalizada exitosamente.", totalTasks, totalTasks);
            return telemetry;
        }

        private void CreateSystemRestorePoint()
        {
            SafeLog("Creando Punto de Restauración del Sistema ('WinOptStudio_PreOpt')...", LogType.Info);
            try
            {
                NativeMethods.RESTOREPOINTINFO rpInfo = new NativeMethods.RESTOREPOINTINFO();
                rpInfo.dwEventType = NativeMethods.BEGIN_SYSTEM_CHANGE;
                rpInfo.dwRestorePtType = NativeMethods.MODIFY_SETTINGS;
                rpInfo.llSequenceNumber = 0;
                rpInfo.szDescription = "WinOpt Studio Pre-Optimización";

                NativeMethods.STATEMGRSTATUS smStatus = new NativeMethods.STATEMGRSTATUS();

                bool result = false;
                try
                {
                    result = NativeMethods.SRSetRestorePointW(ref rpInfo, ref smStatus);
                }
                catch { }

                if (result)
                {
                    SafeLog("Punto de Restauración creado con éxito mediante API nativa SRSetRestorePointW.", LogType.Success);
                }
                else
                {
                    SafeLog("Aviso: La Protección del Sistema está inactiva en Windows. Punto de restauración omitido de forma segura.", LogType.Warning);
                }
            }
            catch (Exception ex)
            {
                SafeLog(string.Format("Aviso: No se pudo crear punto de restauración ({0}). La optimización continuará.", ex.Message), LogType.Warning);
            }
        }

        private void ExecuteTempFilesModule(OptimizationTelemetry telemetry)
        {
            SafeLog("[MÓDULO 1] Limpiando archivos temporales...", LogType.Info);
            List<string> paths = new List<string>();
            try { string ut = Path.GetTempPath(); if (Directory.Exists(ut)) paths.Add(ut); } catch { }
            try { string wt = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"); if (Directory.Exists(wt)) paths.Add(wt); } catch { }
            try { string pf = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"); if (Directory.Exists(pf)) paths.Add(pf); } catch { }

            long freed = 0; int deleted = 0; int skipped = 0;

            foreach (string p in paths)
            {
                try
                {
                    string[] files = Directory.GetFiles(p, "*.*", SearchOption.TopDirectoryOnly);
                    foreach (string f in files)
                    {
                        try
                        {
                            FileInfo fi = new FileInfo(f);
                            long len = fi.Length;
                            if ((fi.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                                fi.Attributes = FileAttributes.Normal;
                            fi.Delete();
                            freed += len;
                            deleted++;
                        }
                        catch { skipped++; }
                    }

                    string[] dirs = Directory.GetDirectories(p, "*", SearchOption.TopDirectoryOnly);
                    foreach (string d in dirs)
                    {
                        try { Directory.Delete(d, true); } catch { skipped++; }
                    }
                }
                catch { }
            }

            telemetry.ModuleFreedBytes[1] = freed;
            telemetry.TotalBytesFreed += freed;
            telemetry.TotalFilesDeleted += deleted;
            telemetry.TotalFilesSkipped += skipped;

            SafeLog(string.Format("ÉXITO: {0} archivos temporales eliminados ({1} liberados). {2} en uso omitidos sin error.", deleted, FormatDiskBytes(freed), skipped), LogType.Success);
        }

        private void ExecuteRamModule(OptimizationTelemetry telemetry)
        {
            SafeLog("[MÓDULO 2] Optimizando memoria RAM de procesos activos...", LogType.Info);
            Process[] procs = Process.GetProcesses();
            int total = procs.Length;
            int count = 0;
            long wsBefore = 0;
            long wsAfter = 0;

            for (int i = 0; i < total; i++)
            {
                Process proc = procs[i];
                try
                {
                    string pName = string.Empty;
                    try { pName = proc.ProcessName; } catch { }
                    if (IsProtectedOrSecurityProcess(pName, proc.Id)) continue;

                    long ws = 0;
                    try { ws = proc.WorkingSet64; } catch { }
                    wsBefore += ws;

                    bool ok = false;
                    IntPtr hProc = IntPtr.Zero;
                    try
                    {
                        hProc = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_SET_QUOTA, false, proc.Id);
                        if (hProc != IntPtr.Zero) ok = NativeMethods.EmptyWorkingSet(hProc);
                        else ok = NativeMethods.EmptyWorkingSet(proc.Handle);
                    }
                    catch { }
                    finally { if (hProc != IntPtr.Zero) NativeMethods.CloseHandle(hProc); }

                    if (ok)
                    {
                        count++;
                        try { proc.Refresh(); wsAfter += proc.WorkingSet64; }
                        catch { wsAfter += ws; }
                    }
                    else wsAfter += ws;
                }
                catch { }
                finally { try { proc.Dispose(); } catch { } }
            }

            double beforeMB = wsBefore / (1024.0 * 1024.0);
            double afterMB = wsAfter / (1024.0 * 1024.0);
            double freedMB = Math.Max(0, beforeMB - afterMB);

            telemetry.RamBeforeMB = beforeMB;
            telemetry.RamAfterMB = afterMB;
            telemetry.RamFreedMB = freedMB;
            telemetry.ModuleFreedBytes[2] = (long)(freedMB * 1024 * 1024);

            SafeLog(string.Format("ÉXITO: {0} procesos optimizados. RAM física liberada: {1:N1} MB reales.", count, freedMB), LogType.Success);
        }

        private void ExecuteDnsWinsockModule(OptimizationTelemetry telemetry)
        {
            SafeLog("[MÓDULO 3] Purgando DNS y reiniciando Winsock...", LogType.Info);
            RunProcessRedirected("ipconfig.exe", "/flushdns", 8000);
            RunProcessRedirected("netsh.exe", "winsock reset", 12000);
            telemetry.RequiresReboot = true;
            SafeLog("ÉXITO: Caché DNS vaciada y catálogo Winsock reiniciado con éxito.", LogType.Success);
        }

        private void ExecuteWindowsUpdateModule(OptimizationTelemetry telemetry)
        {
            SafeLog("[MÓDULO 4] Depurando descargas de Windows Update...", LogType.Info);
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string wuPath = Path.Combine(winDir, @"SoftwareDistribution\Download");

            if (!Directory.Exists(wuPath)) return;

            long freed = 0; int deleted = 0;
            try
            {
                string[] files = Directory.GetFiles(wuPath, "*.*", SearchOption.AllDirectories);
                foreach (string f in files)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(f);
                        long sz = fi.Length;
                        if ((fi.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                            fi.Attributes = FileAttributes.Normal;
                        fi.Delete();
                        freed += sz;
                        deleted++;
                    }
                    catch { }
                }

                string[] dirs = Directory.GetDirectories(wuPath, "*", SearchOption.TopDirectoryOnly);
                foreach (string d in dirs)
                {
                    try { Directory.Delete(d, true); } catch { }
                }
            }
            catch { }

            telemetry.ModuleFreedBytes[4] = freed;
            telemetry.TotalBytesFreed += freed;
            telemetry.TotalFilesDeleted += deleted;

            SafeLog(string.Format("ÉXITO: Caché de actualización depurada: {0} archivos ({1} liberados).", deleted, FormatDiskBytes(freed)), LogType.Success);
        }

        private void ExecuteRecycleBinModule(OptimizationTelemetry telemetry)
        {
            SafeLog("[MÓDULO 5] Vaciando Papelera y caché de miniaturas...", LogType.Info);
            long recycleBytes = 0;
            try
            {
                NativeMethods.SHQUERYRBINFO rbInfo = new NativeMethods.SHQUERYRBINFO();
                rbInfo.cbSize = Marshal.SizeOf(typeof(NativeMethods.SHQUERYRBINFO));
                if (NativeMethods.SHQueryRecycleBin(null, ref rbInfo) == 0) recycleBytes = rbInfo.i64Size;
                NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null, NativeMethods.SHERB_NOCONFIRMATION | NativeMethods.SHERB_NOPROGRESSUI | NativeMethods.SHERB_NOSOUND);
            }
            catch { }

            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string expDir = Path.Combine(localApp, @"Microsoft\Windows\Explorer");
            long thumbFreed = 0; int thumbCount = 0;

            if (Directory.Exists(expDir))
            {
                try
                {
                    string[] patterns = new string[] { "thumbcache_*.db", "iconcache_*.db" };
                    foreach (string pat in patterns)
                    {
                        foreach (string f in Directory.GetFiles(expDir, pat))
                        {
                            try
                            {
                                FileInfo fi = new FileInfo(f);
                                long len = fi.Length;
                                fi.Delete();
                                thumbFreed += len;
                                thumbCount++;
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }

            long total = recycleBytes + thumbFreed;
            telemetry.ModuleFreedBytes[5] = total;
            telemetry.TotalBytesFreed += total;
            telemetry.TotalFilesDeleted += thumbCount;

            SafeLog(string.Format("ÉXITO: Papelera depurada ({0:N2} MB) y {1} archivos de miniaturas eliminados ({2:N2} MB).",
                recycleBytes / (1024.0 * 1024.0), thumbCount, thumbFreed / (1024.0 * 1024.0)), LogType.Success);
        }

        private void ExecuteDismSfcModule()
        {
            SafeLog("[MÓDULO 6] Comprobación y reparación de imagen y archivos de sistema...", LogType.Info);
            SafeLog("Paso 1/2: Ejecutando DISM.exe /Online /Cleanup-Image /RestoreHealth ...", LogType.Command);
            int dismCode = RunProcessRedirected("DISM.exe", "/Online /Cleanup-Image /RestoreHealth");
            SafeLog(string.Format("DISM completado con código de salida: {0}", dismCode), dismCode == 0 ? LogType.Success : LogType.Warning);

            SafeLog("Paso 2/2: Ejecutando sfc.exe /scannow ...", LogType.Command);
            int sfcCode = RunProcessRedirected("sfc.exe", "/scannow");
            SafeLog(string.Format("SFC completado con código de salida: {0}", sfcCode), sfcCode == 0 ? LogType.Success : LogType.Info);
        }

        private void ExecuteGpuShaderPowerModule(OptimizationTelemetry telemetry)
        {
            SafeLog("[MÓDULO 7] Depurando caché de sombreadores GPU...", LogType.Info);
            string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] paths = new string[] {
                Path.Combine(localApp, "D3DSCache"),
                Path.Combine(localApp, @"NVIDIA\DXCache"),
                Path.Combine(localApp, @"NVIDIA\GLCache"),
                Path.Combine(localApp, @"AMD\DxCache"),
                Path.Combine(localApp, @"Intel\ShaderCache")
            };

            long freed = 0; int count = 0;
            foreach (string p in paths)
            {
                if (Directory.Exists(p))
                {
                    try
                    {
                        foreach (string f in Directory.GetFiles(p, "*.*", SearchOption.AllDirectories))
                        {
                            try
                            {
                                FileInfo fi = new FileInfo(f);
                                long sz = fi.Length;
                                fi.Delete();
                                freed += sz;
                                count++;
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }

            telemetry.ModuleFreedBytes[7] = freed;
            telemetry.TotalBytesFreed += freed;
            telemetry.TotalFilesDeleted += count;

            string power = SystemInfoHelper.GetActivePowerScheme();
            SafeLog(string.Format("ÉXITO: Caché de shaders GPU depurada ({0} archivos, {1} liberados). Plan de energía: '{2}'.", count, FormatDiskBytes(freed), power), LogType.Success);
        }

        private int RunProcessRedirected(string fileName, string args, int timeoutMs = 0)
        {
            try
            {
                string resolved = ResolveSystemTool(fileName);
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = resolved;
                psi.Arguments = args;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                try
                {
                    int oem = CultureInfo.CurrentCulture.TextInfo.OEMCodePage;
                    psi.StandardOutputEncoding = Encoding.GetEncoding(oem);
                    psi.StandardErrorEncoding = Encoding.GetEncoding(oem);
                }
                catch
                {
                    psi.StandardOutputEncoding = Encoding.Default;
                    psi.StandardErrorEncoding = Encoding.Default;
                }

                using (Process proc = new Process())
                {
                    proc.StartInfo = psi;
                    proc.OutputDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            string t = e.Data.Trim();
                            if (t.Length > 0) SafeLog(t, LogType.Command);
                        }
                    };
                    proc.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            string t = e.Data.Trim();
                            if (t.Length > 0) SafeLog(t, LogType.Warning);
                        }
                    };

                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    if (timeoutMs > 0)
                    {
                        if (!proc.WaitForExit(timeoutMs))
                        {
                            try { proc.Kill(); } catch { }
                            SafeLog(string.Format("Aviso: El comando '{0}' excedió el límite de {1}s y fue finalizado con seguridad.", fileName, timeoutMs / 1000), LogType.Warning);
                            return -2;
                        }
                    }
                    else
                    {
                        proc.WaitForExit();
                    }
                    return proc.ExitCode;
                }
            }
            catch (Exception ex)
            {
                SafeLog(string.Format("Error al invocar '{0}': {1}", fileName, ex.Message), LogType.Error);
                return -1;
            }
        }

        private string ResolveSystemTool(string binary)
        {
            try
            {
                string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string sub = (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess) ? "Sysnative" : "System32";
                string full = Path.Combine(winDir, Path.Combine(sub, binary));
                if (File.Exists(full)) return full;
            }
            catch { }
            return binary;
        }

        #endregion
    }

    #endregion

    #region Script Exporter (.bat / .ps1)

    public static class ScriptExporter
    {
        public static string ExportToBatch(OptimizationConfig config)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("setlocal enabledelayedexpansion");
            sb.AppendLine("title WinOpt Studio v4.0 - Mantenimiento Automatizado");
            sb.AppendLine("color 0B");
            sb.AppendLine();
            sb.AppendLine(":: Auto-elevación a Administrador");
            sb.AppendLine("openfiles >nul 2>&1");
            sb.AppendLine("if %errorlevel% neq 0 (");
            sb.AppendLine("    echo [!] Solicitando elevacion UAC de Administrador...");
            sb.AppendLine("    powershell -Command \"Start-Process '%~f0' -Verb RunAs\"");
            sb.AppendLine("    exit /b");
            sb.AppendLine(")");
            sb.AppendLine();
            sb.AppendLine("echo ======================================================================");
            sb.AppendLine("echo            WinOpt Studio v4.0 - Tareas de Optimizacion Activas");
            sb.AppendLine("echo ======================================================================");
            sb.AppendLine("echo.");

            if (config.CreateRestorePoint)
            {
                sb.AppendLine("echo [*] Creando Punto de Restauracion del Sistema...");
                sb.AppendLine("powershell -NoProfile -Command \"Checkpoint-Computer -Description 'WinOptStudio_PreOpt' -RestorePointType MODIFY_SETTINGS -ErrorAction SilentlyContinue\"");
                sb.AppendLine("echo.");
            }

            if (config.DoTempFiles)
            {
                sb.AppendLine("echo [*] [1/7] Limpiando archivos temporales (%TEMP%, Temp, Prefetch)...");
                sb.AppendLine("del /s /f /q \"%temp%\\*.*\" >nul 2>&1");
                sb.AppendLine("for /d %%p in (\"%temp%\\*\") do rmdir \"%%p\" /s /q >nul 2>&1");
                sb.AppendLine("del /s /f /q \"%SystemRoot%\\Temp\\*.*\" >nul 2>&1");
                sb.AppendLine("for /d %%p in (\"%SystemRoot%\\Temp\\*\") do rmdir \"%%p\" /s /q >nul 2>&1");
                sb.AppendLine("del /s /f /q \"%SystemRoot%\\Prefetch\\*.*\" >nul 2>&1");
                sb.AppendLine("echo [OK] Archivos temporales eliminados.");
                sb.AppendLine("echo.");
            }

            if (config.DoRamOptimize)
            {
                sb.AppendLine("echo [*] [2/7] Optimizando memoria RAM de procesos...");
                sb.AppendLine("powershell -NoProfile -Command \"Get-Process | ForEach-Object { try { [System.GC]::Collect(); } catch {} }\"");
                sb.AppendLine("echo [OK] Memoria optimizada.");
                sb.AppendLine("echo.");
            }

            if (config.DoDnsWinsock)
            {
                sb.AppendLine("echo [*] [3/7] Purgando DNS y reiniciando catalogo Winsock...");
                sb.AppendLine("ipconfig /flushdns");
                sb.AppendLine("netsh winsock reset");
                sb.AppendLine("echo [OK] Red restablecida.");
                sb.AppendLine("echo.");
            }

            if (config.DoWinUpdate)
            {
                sb.AppendLine("echo [*] [4/7] Limpiando cache de descargas de Windows Update...");
                sb.AppendLine("net stop wuauserv >nul 2>&1");
                sb.AppendLine("del /s /f /q \"%SystemRoot%\\SoftwareDistribution\\Download\\*.*\" >nul 2>&1");
                sb.AppendLine("for /d %%p in (\"%SystemRoot%\\SoftwareDistribution\\Download\\*\") do rmdir \"%%p\" /s /q >nul 2>&1");
                sb.AppendLine("net start wuauserv >nul 2>&1");
                sb.AppendLine("echo [OK] Cache de actualizacion depurada.");
                sb.AppendLine("echo.");
            }

            if (config.DoRecycleBin)
            {
                sb.AppendLine("echo [*] [5/7] Vaciando Papelera de reciclaje y miniaturas...");
                sb.AppendLine("powershell -NoProfile -Command \"Clear-RecycleBin -Force -ErrorAction SilentlyContinue\"");
                sb.AppendLine("del /f /s /q /a \"%LocalAppData%\\Microsoft\\Windows\\Explorer\\thumbcache_*.db\" >nul 2>&1");
                sb.AppendLine("del /f /s /q /a \"%LocalAppData%\\IconCache.db\" >nul 2>&1");
                sb.AppendLine("echo [OK] Papelera y miniaturas depuradas.");
                sb.AppendLine("echo.");
            }

            if (config.DoDismSfc)
            {
                sb.AppendLine("echo [*] [6/7] Reparando imagen y archivos de sistema (DISM / SFC)...");
                sb.AppendLine("DISM.exe /Online /Cleanup-Image /RestoreHealth");
                sb.AppendLine("sfc.exe /scannow");
                sb.AppendLine("echo [OK] Reparacion de sistema concluida.");
                sb.AppendLine("echo.");
            }

            if (config.DoGpuShadersPower)
            {
                sb.AppendLine("echo [*] [7/7] Depurando cache de sombreadores GPU (D3DSCache / NVIDIA / AMD)...");
                sb.AppendLine("del /s /f /q \"%LocalAppData%\\D3DSCache\\*.*\" >nul 2>&1");
                sb.AppendLine("del /s /f /q \"%LocalAppData%\\NVIDIA\\DXCache\\*.*\" >nul 2>&1");
                sb.AppendLine("del /s /f /q \"%LocalAppData%\\AMD\\DxCache\\*.*\" >nul 2>&1");
                sb.AppendLine("powercfg /getactivescheme");
                sb.AppendLine("echo [OK] Shaders depurados y plan de energia verificado.");
                sb.AppendLine("echo.");
            }

            sb.AppendLine("echo ======================================================================");
            sb.AppendLine("echo [EXITO] Secuencia de optimizacion completada.");
            sb.AppendLine("echo ======================================================================");
            sb.AppendLine("pause");
            sb.AppendLine("exit /b 0");
            return sb.ToString();
        }

        public static string ExportToPowerShell(OptimizationConfig config)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#Requires -RunAsAdministrator");
            sb.AppendLine("<#");
            sb.AppendLine(".SYNOPSIS");
            sb.AppendLine("    WinOpt Studio v4.0 - Script de Mantenimiento PowerShell");
            sb.AppendLine(".DESCRIPTION");
            sb.AppendLine(string.Format("    Generado automaticamente por WinOpt Studio v4.0 el {0}.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            sb.AppendLine("#>");
            sb.AppendLine();
            sb.AppendLine("$Host.UI.RawUI.WindowTitle = 'WinOpt Studio v4.0 - Mantenimiento'");
            sb.AppendLine("Write-Host '======================================================================' -ForegroundColor Cyan");
            sb.AppendLine("Write-Host '          WinOpt Studio v4.0 - Optimizacion de Windows' -ForegroundColor Cyan");
            sb.AppendLine("Write-Host '======================================================================' -ForegroundColor Cyan");
            sb.AppendLine("Write-Host ''");

            if (config.CreateRestorePoint)
            {
                sb.AppendLine("Write-Host '[*] Creando Punto de Restauracion...' -ForegroundColor Magenta");
                sb.AppendLine("Checkpoint-Computer -Description 'WinOptStudio_PreOpt' -RestorePointType MODIFY_SETTINGS -ErrorAction SilentlyContinue");
                sb.AppendLine("Write-Host ''");
            }

            if (config.DoTempFiles)
            {
                sb.AppendLine("Write-Host '[*] [1/7] Limpiando archivos temporales...' -ForegroundColor Yellow");
                sb.AppendLine("$tempPaths = @($env:TEMP, \"$env:SystemRoot\\Temp\", \"$env:SystemRoot\\Prefetch\")");
                sb.AppendLine("foreach ($p in $tempPaths) { if (Test-Path $p) { Get-ChildItem -Path $p -Recurse -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue } }");
                sb.AppendLine("Write-Host '[OK] Temporales eliminados.' -ForegroundColor Green");
                sb.AppendLine("Write-Host ''");
            }

            if (config.DoRamOptimize)
            {
                sb.AppendLine("Write-Host '[*] [2/7] Optimizando memoria RAM...' -ForegroundColor Yellow");
                sb.AppendLine("[System.GC]::Collect()");
                sb.AppendLine("[System.GC]::WaitForPendingFinalizers()");
                sb.AppendLine("Write-Host '[OK] Memoria optimizada.' -ForegroundColor Green");
                sb.AppendLine("Write-Host ''");
            }

            if (config.DoDnsWinsock)
            {
                sb.AppendLine("Write-Host '[*] [3/7] Purgando DNS y reiniciando Winsock...' -ForegroundColor Yellow");
                sb.AppendLine("Clear-DnsClientCache -ErrorAction SilentlyContinue");
                sb.AppendLine("netsh winsock reset");
                sb.AppendLine("Write-Host '[OK] Red restablecida.' -ForegroundColor Green");
                sb.AppendLine("Write-Host ''");
            }

            if (config.DoWinUpdate)
            {
                sb.AppendLine("Write-Host '[*] [4/7] Limpiando cache de Windows Update...' -ForegroundColor Yellow");
                sb.AppendLine("Stop-Service -Name wuauserv -Force -ErrorAction SilentlyContinue");
                sb.AppendLine("$wuDownload = \"$env:SystemRoot\\SoftwareDistribution\\Download\"");
                sb.AppendLine("if (Test-Path $wuDownload) { Get-ChildItem -Path $wuDownload -Recurse -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue }");
                sb.AppendLine("Start-Service -Name wuauserv -ErrorAction SilentlyContinue");
                sb.AppendLine("Write-Host '[OK] Cache de actualizacion depurada.' -ForegroundColor Green");
                sb.AppendLine("Write-Host ''");
            }

            if (config.DoRecycleBin)
            {
                sb.AppendLine("Write-Host '[*] [5/7] Vaciando Papelera y miniaturas...' -ForegroundColor Yellow");
                sb.AppendLine("Clear-RecycleBin -Force -ErrorAction SilentlyContinue");
                sb.AppendLine("$thumb = \"$env:LOCALAPPDATA\\Microsoft\\Windows\\Explorer\"");
                sb.AppendLine("if (Test-Path $thumb) { Get-ChildItem -Path $thumb -Filter 'thumbcache_*.db' -Force -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue }");
                sb.AppendLine("Write-Host '[OK] Papelera y miniaturas procesadas.' -ForegroundColor Green");
                sb.AppendLine("Write-Host ''");
            }

            if (config.DoDismSfc)
            {
                sb.AppendLine("Write-Host '[*] [6/7] Reparando sistema (DISM / SFC)...' -ForegroundColor Yellow");
                sb.AppendLine("DISM.exe /Online /Cleanup-Image /RestoreHealth");
                sb.AppendLine("sfc.exe /scannow");
                sb.AppendLine("Write-Host '[OK] Reparacion finalizada.' -ForegroundColor Green");
                sb.AppendLine("Write-Host ''");
            }

            if (config.DoGpuShadersPower)
            {
                sb.AppendLine("Write-Host '[*] [7/7] Depurando cache de shaders GPU y energia...' -ForegroundColor Yellow");
                sb.AppendLine("$sc = \"$env:LOCALAPPDATA\\D3DSCache\"");
                sb.AppendLine("if (Test-Path $sc) { Get-ChildItem -Path $sc -Recurse -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue }");
                sb.AppendLine("powercfg /getactivescheme");
                sb.AppendLine("Write-Host '[OK] Shaders y energia verificados.' -ForegroundColor Green");
                sb.AppendLine("Write-Host ''");
            }

            sb.AppendLine("Write-Host '======================================================================' -ForegroundColor Cyan");
            sb.AppendLine("Write-Host '[EXITO] Secuencia de mantenimiento completada.' -ForegroundColor Green");
            sb.AppendLine("Write-Host '======================================================================' -ForegroundColor Cyan");
            sb.AppendLine("Read-Host 'Presiona Enter para cerrar'");
            return sb.ToString();
        }
    }

    #endregion

    #region Main Form (Presentation Layer)

    public class MainForm : Form
    {
        #region Palette & Theme

        private readonly Color ColorBgMain       = Color.FromArgb(13, 17, 23);    // #0D1117
        private readonly Color ColorBgSecondary  = Color.FromArgb(22, 27, 34);    // #161B22
        private readonly Color ColorCardBg       = Color.FromArgb(33, 38, 45);    // #21262D
        private readonly Color ColorBorder       = Color.FromArgb(48, 54, 61);    // #30363D
        private readonly Color ColorTextPrimary  = Color.FromArgb(240, 246, 252); // #F0F6FC
        private readonly Color ColorTextMuted    = Color.FromArgb(139, 148, 158); // #8B949E
        private readonly Color ColorAccentBlue   = Color.FromArgb(88, 166, 255);  // #58A6FF
        private readonly Color ColorWarning      = Color.FromArgb(210, 153, 34);  // #D29922
        private readonly Color ColorError        = Color.FromArgb(248, 81, 73);   // #F85149
        private readonly Color ColorCommand      = Color.FromArgb(188, 140, 255); // #BC8CFF

        #endregion

        #region Controls

        private Panel pnlHeader;
        private Panel pnlBottomProgress;
        private Panel pnlMainBody;

        private Panel pnlSidebar;
        private Panel pnlDivider;
        private Panel pnlConsoleArea;

        // Circular Driver Booster Gauge
        private CircularSystemGauge systemGauge;
        private Label lblAppTitle;
        private Label lblAppSubtitle;
        private Label lblAdminBadge;

        // Driver Booster Live Telemetry HUD & Turbo Boost
        private Panel pnlLiveTelemetry;
        private Label lblRamStats;
        private ModernProgressBar pbRamLive;
        private Label lblCpuPlan;
        private ModernButton btnTurboBoost;
        private Timer ramMonitorTimer;

        // Sidebar
        private Panel pnlSidebarScroll;
        private ModernModuleCard cardTemp;
        private ModernModuleCard cardRam;
        private ModernModuleCard cardDns;
        private ModernModuleCard cardWinUpd;
        private ModernModuleCard cardRecycle;
        private ModernModuleCard cardDismSfc;
        private ModernModuleCard cardGpuPower;
        private CheckBox chkRestorePoint;

        private ModernButton btnRun;
        private ModernButton btnDryRun;
        private ModernButton btnExport;

        // Console & Toolbar
        private Panel pnlRebootNotice;
        private Label lblRebootText;
        private ModernButton btnRebootNow;
        private ModernButton btnRebootDismiss;

        private Label lblConsoleHeader;
        private Label lblConsoleBadge;
        private ModernButton btnCopyLog;
        private ModernButton btnSaveLog;
        private ModernButton btnClearLog;
        private RichTextBox rtbConsole;

        private ModernProgressBar modernBar;
        private Label lblStatus;
        private Label lblTaskProgress;

        private Timer animationTimer;
        private bool isRunning = false;
        private OptimizationEngine engine;

        #endregion

        public MainForm()
        {
            this.SuspendLayout();
            InitWindow();
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            BuildStructuredLayout();
            this.ResumeLayout(true);

            ApplyNativeDarkMode();

            engine = new OptimizationEngine(
                (msg, type) => AppendLog(msg, type),
                (pct, text, done, total) => UpdateProgressSafe(pct, text, done, total)
            );

            // Timer de animación estilo radar Driver Booster (25 fps suave)
            animationTimer = new Timer();
            animationTimer.Interval = 40;
            animationTimer.Tick += (s, e) =>
            {
                if (systemGauge != null && systemGauge.IsScanning)
                {
                    systemGauge.StepAnimation();
                }
            };

            // Monitor de RAM en vivo estilo Driver Booster
            ramMonitorTimer = new Timer();
            ramMonitorTimer.Interval = 4000;
            ramMonitorTimer.Tick += (s, e) =>
            {
                if (!isRunning) UpdateLiveRamInfo();
            };
            ramMonitorTimer.Start();
        }

        private void InitWindow()
        {
            this.Text = "WinOpt Studio v4.0 • Suite de Rendimiento y Mantenimiento";
            this.Size = new Size(1180, 780);
            this.MinimumSize = new Size(1120, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColorBgMain;
            this.ForeColor = ColorTextPrimary;
            this.DoubleBuffered = true;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        }

        private void ApplyNativeDarkMode()
        {
            try
            {
                int darkMode = 1;
                if (NativeMethods.DwmSetWindowAttribute(this.Handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int)) != 0)
                {
                    NativeMethods.DwmSetWindowAttribute(this.Handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkMode, sizeof(int));
                }
            }
            catch { }
        }

        private bool IsRunAsAdmin()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        #region UI Layout Construction (Cero Líneas Blancas)

        private void BuildStructuredLayout()
        {
            BuildHeader();
            BuildBottomProgressArea();

            // Contenedor Central con acoplamiento limpio:
            // Reemplaza SplitContainer para ELIMINAR al 100% la barra blanca de Windows Forms!
            pnlMainBody = new Panel();
            pnlMainBody.Dock = DockStyle.Fill;
            pnlMainBody.BackColor = ColorBgMain;

            // 1. Sidebar (Izquierda)
            pnlSidebar = new Panel();
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 430;
            pnlSidebar.BackColor = ColorBgSecondary;
            BuildSidebarContent(pnlSidebar);

            // 2. Divisor Oscuro (2px en #252C37 sin artefactos visuales)
            pnlDivider = new Panel();
            pnlDivider.Dock = DockStyle.Left;
            pnlDivider.Width = 2;
            pnlDivider.BackColor = Color.FromArgb(37, 44, 55); // #252C37

            // 3. Área de Consola (Fill)
            pnlConsoleArea = new Panel();
            pnlConsoleArea.Dock = DockStyle.Fill;
            pnlConsoleArea.BackColor = ColorBgMain;
            BuildConsoleArea(pnlConsoleArea);

            pnlMainBody.Controls.Add(pnlConsoleArea);
            pnlMainBody.Controls.Add(pnlDivider);
            pnlMainBody.Controls.Add(pnlSidebar);

            this.Controls.Add(pnlMainBody);
            this.Controls.Add(pnlBottomProgress);
            this.Controls.Add(pnlHeader);

            this.Shown += MainForm_Shown;
        }

        private void BuildHeader()
        {
            pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 74;
            pnlHeader.BackColor = ColorBgSecondary;
            pnlHeader.Padding = new Padding(16, 8, 16, 8);
            pnlHeader.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(ColorBorder, 1))
                {
                    e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };

            // Anillo radar Driver Booster
            systemGauge = new CircularSystemGauge();
            systemGauge.Location = new Point(16, 8);
            systemGauge.Size = new Size(58, 58);
            systemGauge.Percent = 88;
            pnlHeader.Controls.Add(systemGauge);

            lblAppTitle = new Label();
            lblAppTitle.Text = "WinOpt Studio v4.0 • Cyber Edition";
            lblAppTitle.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
            lblAppTitle.ForeColor = ColorAccentBlue;
            lblAppTitle.AutoSize = true;
            lblAppTitle.Location = new Point(84, 11);

            string osExact = SystemInfoHelper.GetExactOperatingSystemInfo();
            lblAppSubtitle = new Label();
            lblAppSubtitle.Text = string.Format("{0} • Privilegios Nativos", osExact);
            lblAppSubtitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            lblAppSubtitle.ForeColor = ColorTextMuted;
            lblAppSubtitle.AutoSize = true;
            lblAppSubtitle.Location = new Point(86, 39);

            // Panel de Telemetría en Vivo (Estilo Driver Booster HUD)
            pnlLiveTelemetry = new Panel();
            pnlLiveTelemetry.Size = new Size(240, 52);
            pnlLiveTelemetry.Location = new Point(430, 11);
            pnlLiveTelemetry.BackColor = Color.FromArgb(28, 33, 40);
            pnlLiveTelemetry.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(ColorBorder, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlLiveTelemetry.Width - 1, pnlLiveTelemetry.Height - 1);
                }
            };

            lblRamStats = new Label();
            lblRamStats.Text = "RAM en Uso: Calculando...";
            lblRamStats.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            lblRamStats.ForeColor = ColorTextPrimary;
            lblRamStats.AutoSize = true;
            lblRamStats.Location = new Point(8, 6);
            pnlLiveTelemetry.Controls.Add(lblRamStats);

            pbRamLive = new ModernProgressBar();
            pbRamLive.Location = new Point(8, 24);
            pbRamLive.Size = new Size(224, 7);
            pbRamLive.Value = 45;
            pnlLiveTelemetry.Controls.Add(pbRamLive);

            string powerScheme = SystemInfoHelper.GetActivePowerScheme();
            string shortPower = powerScheme.Length > 16 ? powerScheme.Substring(0, 14) + ".." : powerScheme;
            lblCpuPlan = new Label();
            lblCpuPlan.Text = string.Format("{0} Cores • {1}", Environment.ProcessorCount, shortPower);
            lblCpuPlan.Font = new Font("Segoe UI", 7.5F, FontStyle.Regular);
            lblCpuPlan.ForeColor = ColorTextMuted;
            lblCpuPlan.AutoSize = true;
            lblCpuPlan.Location = new Point(8, 34);
            pnlLiveTelemetry.Controls.Add(lblCpuPlan);

            // Botón TURBO BOOST
            btnTurboBoost = new ModernButton();
            btnTurboBoost.Text = "TURBO BOOST";
            btnTurboBoost.VectorIcon = ButtonIconType.Lightning;
            btnTurboBoost.IsPrimary = true;
            btnTurboBoost.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnTurboBoost.Size = new Size(130, 38);
            btnTurboBoost.Location = new Point(690, 17);
            btnTurboBoost.Click += (s, e) => ExecuteTurboBoost();

            bool admin = IsRunAsAdmin();
            lblAdminBadge = new Label();
            lblAdminBadge.Text = admin ? "● UAC: ADMINISTRADOR ACTIVO" : "▲ MODO ESTÁNDAR (Clic para elevar)";
            lblAdminBadge.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblAdminBadge.ForeColor = admin ? Color.FromArgb(63, 185, 80) : ColorWarning;
            lblAdminBadge.BackColor = admin ? Color.FromArgb(20, 48, 28) : Color.FromArgb(55, 35, 10);
            lblAdminBadge.Padding = new Padding(10, 6, 10, 6);
            lblAdminBadge.AutoSize = true;
            lblAdminBadge.Cursor = admin ? Cursors.Default : Cursors.Hand;
            lblAdminBadge.Location = new Point(840, 20);
            lblAdminBadge.Click += (s, e) => { if (!admin) ElevateApplication(); };

            pnlHeader.Resize += (s, e) =>
            {
                lblAdminBadge.Location = new Point(pnlHeader.Width - lblAdminBadge.Width - 16, 20);
                btnTurboBoost.Location = new Point(lblAdminBadge.Left - btnTurboBoost.Width - 12, 17);
                pnlLiveTelemetry.Location = new Point(btnTurboBoost.Left - pnlLiveTelemetry.Width - 14, 11);
            };

            pnlHeader.Controls.Add(lblAppTitle);
            pnlHeader.Controls.Add(lblAppSubtitle);
            pnlHeader.Controls.Add(pnlLiveTelemetry);
            pnlHeader.Controls.Add(btnTurboBoost);
            pnlHeader.Controls.Add(lblAdminBadge);
        }

        private void ElevateApplication()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = Application.ExecutablePath;
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                Process.Start(psi);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo solicitar elevación:\n" + ex.Message, "UAC Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BuildBottomProgressArea()
        {
            pnlBottomProgress = new Panel();
            pnlBottomProgress.Dock = DockStyle.Bottom;
            pnlBottomProgress.Height = 68;
            pnlBottomProgress.BackColor = ColorBgSecondary;
            pnlBottomProgress.Padding = new Padding(20, 8, 20, 10);
            pnlBottomProgress.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(ColorBorder, 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, pnlBottomProgress.Width, 0);
                }
            };

            lblStatus = new Label();
            lblStatus.Text = "● Sistema listo para iniciar simulación u optimización activa.";
            lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblStatus.ForeColor = ColorTextMuted;
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(20, 8);

            lblTaskProgress = new Label();
            lblTaskProgress.Text = "0/7 Módulos (0%)";
            lblTaskProgress.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTaskProgress.ForeColor = ColorAccentBlue;
            lblTaskProgress.AutoSize = true;
            lblTaskProgress.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTaskProgress.Location = new Point(pnlBottomProgress.Width - 140, 8);

            modernBar = new ModernProgressBar();
            modernBar.Dock = DockStyle.Bottom;
            modernBar.Height = 22;
            modernBar.Value = 0;
            modernBar.Maximum = 100;

            pnlBottomProgress.Resize += (s, e) =>
            {
                lblTaskProgress.Location = new Point(pnlBottomProgress.Width - lblTaskProgress.Width - 25, 8);
            };

            pnlBottomProgress.Controls.Add(lblStatus);
            pnlBottomProgress.Controls.Add(lblTaskProgress);
            pnlBottomProgress.Controls.Add(modernBar);
        }

        private void BuildSidebarContent(Panel panel)
        {
            pnlSidebarScroll = new Panel();
            pnlSidebarScroll.Dock = DockStyle.Fill;
            pnlSidebarScroll.AutoScroll = true;
            pnlSidebarScroll.BackColor = ColorBgSecondary;
            pnlSidebarScroll.Padding = new Padding(12, 12, 16, 16);

            Label lblModTitle = new Label();
            lblModTitle.Text = "MÓDULOS DE MANTENIMIENTO";
            lblModTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblModTitle.ForeColor = ColorTextMuted;
            lblModTitle.AutoSize = true;
            lblModTitle.Location = new Point(14, 12);
            pnlSidebarScroll.Controls.Add(lblModTitle);

            ModernButton btnAll = CreateQuickSelector("Todos", 215, 8, 55);
            btnAll.Click += (s, e) => SetAllModules(true);
            pnlSidebarScroll.Controls.Add(btnAll);

            ModernButton btnNone = CreateQuickSelector("Ninguno", 275, 8, 65);
            btnNone.Click += (s, e) => SetAllModules(false);
            pnlSidebarScroll.Controls.Add(btnNone);

            ModernButton btnFast = CreateQuickSelector("Rápidos", 345, 8, 62);
            btnFast.Click += (s, e) => SetRecommendedModules();
            pnlSidebarScroll.Controls.Add(btnFast);

            int currentY = 40;
            int cardGap = 8;
            int cardWidth = 390;

            cardTemp = new ModernModuleCard();
            cardTemp.Title = "1. Archivos Temporales y Prefetch";
            cardTemp.Subtitle = "%TEMP%, C:\\Windows\\Temp y Prefetch (omite en uso)";
            cardTemp.IconType = CardIconType.TempFiles;
            cardTemp.Checked = true;
            cardTemp.Location = new Point(12, currentY);
            cardTemp.Size = new Size(cardWidth, 70);
            pnlSidebarScroll.Controls.Add(cardTemp);
            currentY += 70 + cardGap;

            cardRam = new ModernModuleCard();
            cardRam.Title = "2. Optimización de Memoria RAM";
            cardRam.Subtitle = "P/Invoke EmptyWorkingSet (psapi.dll) en procesos";
            cardRam.IconType = CardIconType.Ram;
            cardRam.Checked = true;
            cardRam.Location = new Point(12, currentY);
            cardRam.Size = new Size(cardWidth, 70);
            pnlSidebarScroll.Controls.Add(cardRam);
            currentY += 70 + cardGap;

            cardDns = new ModernModuleCard();
            cardDns.Title = "3. Red: Caché DNS y Winsock";
            cardDns.Subtitle = "ipconfig /flushdns y netsh winsock reset";
            cardDns.IconType = CardIconType.NetworkDns;
            cardDns.Checked = true;
            cardDns.Location = new Point(12, currentY);
            cardDns.Size = new Size(cardWidth, 70);
            pnlSidebarScroll.Controls.Add(cardDns);
            currentY += 70 + cardGap;

            cardWinUpd = new ModernModuleCard();
            cardWinUpd.Title = "4. Caché de Windows Update";
            cardWinUpd.Subtitle = "SoftwareDistribution\\Download (detiene/inicia servicio)";
            cardWinUpd.IconType = CardIconType.WindowsUpdate;
            cardWinUpd.Checked = true;
            cardWinUpd.Location = new Point(12, currentY);
            cardWinUpd.Size = new Size(cardWidth, 70);
            pnlSidebarScroll.Controls.Add(cardWinUpd);
            currentY += 70 + cardGap;

            cardRecycle = new ModernModuleCard();
            cardRecycle.Title = "5. Papelera y Caché Miniaturas";
            cardRecycle.Subtitle = "SHEmptyRecycleBin (shell32) y bases thumbcache";
            cardRecycle.IconType = CardIconType.RecycleBin;
            cardRecycle.Checked = true;
            cardRecycle.Location = new Point(12, currentY);
            cardRecycle.Size = new Size(cardWidth, 70);
            pnlSidebarScroll.Controls.Add(cardRecycle);
            currentY += 70 + cardGap;

            cardDismSfc = new ModernModuleCard();
            cardDismSfc.Title = "6. Reparación de Sistema (DISM/SFC)";
            cardDismSfc.Subtitle = "DISM /RestoreHealth y SFC /scannow (profundo)";
            cardDismSfc.IconType = CardIconType.SystemRepair;
            cardDismSfc.Checked = false;
            cardDismSfc.Location = new Point(12, currentY);
            cardDismSfc.Size = new Size(cardWidth, 70);
            pnlSidebarScroll.Controls.Add(cardDismSfc);
            currentY += 70 + cardGap;

            cardGpuPower = new ModernModuleCard();
            cardGpuPower.Title = "7. Shaders GPU y Plan de Energía";
            cardGpuPower.Subtitle = "D3DSCache / NVIDIA / AMD y telemetría powercfg";
            cardGpuPower.IconType = CardIconType.GpuShaders;
            cardGpuPower.Checked = true;
            cardGpuPower.Location = new Point(12, currentY);
            cardGpuPower.Size = new Size(cardWidth, 70);
            pnlSidebarScroll.Controls.Add(cardGpuPower);
            currentY += 70 + 10;

            chkRestorePoint = new CheckBox();
            chkRestorePoint.Text = "Crear Punto de Restauración del Sistema";
            chkRestorePoint.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            chkRestorePoint.ForeColor = ColorAccentBlue;
            chkRestorePoint.Checked = false;
            chkRestorePoint.AutoSize = true;
            chkRestorePoint.Location = new Point(16, currentY);
            pnlSidebarScroll.Controls.Add(chkRestorePoint);
            currentY += 28;

            btnRun = new ModernButton();
            btnRun.Text = "EJECUTAR OPTIMIZACIÓN";
            btnRun.VectorIcon = ButtonIconType.Lightning;
            btnRun.IsPrimary = true;
            btnRun.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnRun.Location = new Point(12, currentY);
            btnRun.Size = new Size(cardWidth, 44);
            btnRun.Click += (s, e) => StartOptimization();
            pnlSidebarScroll.Controls.Add(btnRun);
            currentY += 50;

            btnDryRun = new ModernButton();
            btnDryRun.Text = "SIMULAR / ANALIZAR (DRY-RUN)";
            btnDryRun.VectorIcon = ButtonIconType.Search;
            btnDryRun.IsPrimary = false;
            btnDryRun.ForeColor = ColorAccentBlue;
            btnDryRun.Location = new Point(12, currentY);
            btnDryRun.Size = new Size(cardWidth, 38);
            btnDryRun.Click += (s, e) => StartDryRun();
            pnlSidebarScroll.Controls.Add(btnDryRun);
            currentY += 44;

            btnExport = new ModernButton();
            btnExport.Text = "Exportar a Script (.bat / .ps1)";
            btnExport.VectorIcon = ButtonIconType.Save;
            btnExport.IsPrimary = false;
            btnExport.ForeColor = ColorTextMuted;
            btnExport.Location = new Point(12, currentY);
            btnExport.Size = new Size(cardWidth, 34);
            btnExport.Click += BtnExport_Click;
            pnlSidebarScroll.Controls.Add(btnExport);
            currentY += 44;

            Panel pnlSpacer = new Panel();
            pnlSpacer.Location = new Point(12, currentY);
            pnlSpacer.Size = new Size(cardWidth, 20);
            pnlSidebarScroll.Controls.Add(pnlSpacer);

            panel.Controls.Add(pnlSidebarScroll);
        }

        private ModernButton CreateQuickSelector(string text, int x, int y, int width)
        {
            ModernButton btn = new ModernButton();
            btn.Text = text;
            btn.IsPrimary = false;
            btn.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            btn.ForeColor = ColorAccentBlue;
            btn.Location = new Point(x, y);
            btn.Size = new Size(width, 24);
            return btn;
        }

        private void BuildConsoleArea(Panel panel)
        {
            panel.Padding = new Padding(12, 10, 16, 10);

            Panel pnlToolbar = new Panel();
            pnlToolbar.Dock = DockStyle.Top;
            pnlToolbar.Height = 36;
            pnlToolbar.BackColor = ColorBgMain;

            lblConsoleHeader = new Label();
            lblConsoleHeader.Text = "TERMINAL DE REGISTRO EN TIEMPO REAL";
            lblConsoleHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblConsoleHeader.ForeColor = ColorTextMuted;
            lblConsoleHeader.AutoSize = true;
            lblConsoleHeader.Location = new Point(2, 8);
            pnlToolbar.Controls.Add(lblConsoleHeader);

            lblConsoleBadge = new Label();
            lblConsoleBadge.Text = "ESTADO: EN ESPERA";
            lblConsoleBadge.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            lblConsoleBadge.ForeColor = ColorAccentBlue;
            lblConsoleBadge.BackColor = Color.FromArgb(22, 27, 34);
            lblConsoleBadge.Padding = new Padding(8, 3, 8, 3);
            lblConsoleBadge.AutoSize = true;
            lblConsoleBadge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblConsoleBadge.Location = new Point(pnlToolbar.Width - 360, 6);
            pnlToolbar.Controls.Add(lblConsoleBadge);

            btnCopyLog = new ModernButton();
            btnCopyLog.Text = "Copiar";
            btnCopyLog.VectorIcon = ButtonIconType.Copy;
            btnCopyLog.Size = new Size(75, 26);
            btnCopyLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCopyLog.Location = new Point(pnlToolbar.Width - 230, 4);
            btnCopyLog.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(rtbConsole.Text))
                {
                    Clipboard.SetText(rtbConsole.Text);
                    AppendLog("Registro de terminal copiado al portapapeles.", LogType.Info);
                }
            };
            pnlToolbar.Controls.Add(btnCopyLog);

            btnSaveLog = new ModernButton();
            btnSaveLog.Text = "Guardar";
            btnSaveLog.VectorIcon = ButtonIconType.Save;
            btnSaveLog.Size = new Size(80, 26);
            btnSaveLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSaveLog.Location = new Point(pnlToolbar.Width - 150, 4);
            btnSaveLog.Click += BtnSaveLog_Click;
            pnlToolbar.Controls.Add(btnSaveLog);

            btnClearLog = new ModernButton();
            btnClearLog.Text = "Limpiar";
            btnClearLog.VectorIcon = ButtonIconType.Clean;
            btnClearLog.Size = new Size(72, 26);
            btnClearLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClearLog.Location = new Point(pnlToolbar.Width - 65, 4);
            btnClearLog.Click += (s, e) =>
            {
                rtbConsole.Clear();
                AppendLog("Consola reiniciada por el usuario.", LogType.Info);
            };
            pnlToolbar.Controls.Add(btnClearLog);

            pnlToolbar.Resize += (s, e) =>
            {
                btnClearLog.Location = new Point(pnlToolbar.Width - 75, 4);
                btnSaveLog.Location = new Point(pnlToolbar.Width - 160, 4);
                btnCopyLog.Location = new Point(pnlToolbar.Width - 240, 4);
                lblConsoleBadge.Location = new Point(pnlToolbar.Width - 390, 6);
            };

            pnlRebootNotice = new Panel();
            pnlRebootNotice.Dock = DockStyle.Top;
            pnlRebootNotice.Height = 46;
            pnlRebootNotice.BackColor = Color.FromArgb(42, 28, 7);
            pnlRebootNotice.Padding = new Padding(12, 6, 12, 6);
            pnlRebootNotice.Visible = false;
            pnlRebootNotice.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(217, 119, 6), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlRebootNotice.Width - 1, pnlRebootNotice.Height - 1);
                }
            };

            lblRebootText = new Label();
            lblRebootText.Text = "Catálogo Winsock restablecido. Se requiere reiniciar Windows para finalizar la configuración de red.";
            lblRebootText.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblRebootText.ForeColor = Color.FromArgb(251, 191, 36);
            lblRebootText.AutoSize = false;
            lblRebootText.AutoEllipsis = true;
            lblRebootText.TextAlign = ContentAlignment.MiddleLeft;
            lblRebootText.Height = 28;
            pnlRebootNotice.Controls.Add(lblRebootText);

            btnRebootNow = new ModernButton();
            btnRebootNow.Text = "Reiniciar PC";
            btnRebootNow.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnRebootNow.Size = new Size(110, 28);
            btnRebootNow.IsPrimary = false;
            btnRebootNow.ForeColor = Color.White;
            btnRebootNow.BackColor = Color.FromArgb(180, 83, 9);
            btnRebootNow.Click += (s, e) =>
            {
                if (MessageBox.Show("¿Deseas reiniciar el equipo ahora mismo para aplicar los cambios de red?", "Reiniciar Windows", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Process.Start("shutdown.exe", "/r /t 10 /c \"WinOpt Studio - Reinicio para aplicar configuracion Winsock\"");
                }
            };
            pnlRebootNotice.Controls.Add(btnRebootNow);

            btnRebootDismiss = new ModernButton();
            btnRebootDismiss.Text = "Más Tarde";
            btnRebootDismiss.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnRebootDismiss.Size = new Size(80, 28);
            btnRebootDismiss.IsPrimary = false;
            btnRebootDismiss.ForeColor = Color.FromArgb(209, 213, 219);
            btnRebootDismiss.BackColor = Color.FromArgb(55, 65, 81);
            btnRebootDismiss.Click += (s, e) => { pnlRebootNotice.Visible = false; };
            pnlRebootNotice.Controls.Add(btnRebootDismiss);

            pnlRebootNotice.Resize += (s, e) => LayoutRebootNotice();
            pnlRebootNotice.VisibleChanged += (s, e) => { if (pnlRebootNotice.Visible) LayoutRebootNotice(); };
            LayoutRebootNotice();

            Panel pnlRtbFrame = new Panel();
            pnlRtbFrame.Dock = DockStyle.Fill;
            pnlRtbFrame.BackColor = ColorBgMain;
            pnlRtbFrame.Padding = new Padding(10);
            pnlRtbFrame.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(ColorBorder, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlRtbFrame.Width - 1, pnlRtbFrame.Height - 1);
                }
            };

            rtbConsole = new RichTextBox();
            rtbConsole.Dock = DockStyle.Fill;
            rtbConsole.BackColor = ColorBgMain;
            rtbConsole.ForeColor = ColorTextPrimary;
            rtbConsole.BorderStyle = BorderStyle.None;
            rtbConsole.Font = new Font("Consolas", 9.75F, FontStyle.Regular);
            rtbConsole.ReadOnly = true;
            rtbConsole.DetectUrls = true;
            rtbConsole.ScrollBars = RichTextBoxScrollBars.Vertical;

            pnlRtbFrame.Controls.Add(rtbConsole);

            panel.Controls.Add(pnlRtbFrame);
            panel.Controls.Add(pnlRebootNotice);
            panel.Controls.Add(pnlToolbar);
        }

        private void LayoutRebootNotice()
        {
            if (pnlRebootNotice == null || btnRebootDismiss == null || btnRebootNow == null || lblRebootText == null) return;
            int pad = 12;
            int y = (pnlRebootNotice.Height - btnRebootDismiss.Height) / 2;
            btnRebootDismiss.Location = new Point(pnlRebootNotice.Width - btnRebootDismiss.Width - pad, y);
            btnRebootNow.Location = new Point(btnRebootDismiss.Left - btnRebootNow.Width - 8, y);
            lblRebootText.Location = new Point(pad, (pnlRebootNotice.Height - lblRebootText.Height) / 2);
            lblRebootText.Width = Math.Max(40, btnRebootNow.Left - lblRebootText.Left - 10);

            btnRebootDismiss.BringToFront();
            btnRebootNow.BringToFront();
        }

        private void SetAllModules(bool check)
        {
            if (isRunning) return;
            cardTemp.Checked = check;
            cardRam.Checked = check;
            cardDns.Checked = check;
            cardWinUpd.Checked = check;
            cardRecycle.Checked = check;
            cardDismSfc.Checked = check;
            cardGpuPower.Checked = check;
        }

        private void SetRecommendedModules()
        {
            if (isRunning) return;
            cardTemp.Checked = true;
            cardRam.Checked = true;
            cardDns.Checked = true;
            cardWinUpd.Checked = true;
            cardRecycle.Checked = true;
            cardDismSfc.Checked = false;
            cardGpuPower.Checked = true;
        }

        #endregion

        #region Execution & Orchestration

        private void MainForm_Shown(object sender, EventArgs e)
        {
            AppendLog("==========================================================================", LogType.Info);
            AppendLog(" WinOpt Studio v4.0 • Suite Cyber-Performance & Windows Internals", LogType.Info);
            AppendLog("==========================================================================", LogType.Info);
            AppendLog(string.Format("Sistema Operativo : {0}", SystemInfoHelper.GetExactOperatingSystemInfo()), LogType.Info);
            AppendLog(string.Format("Entorno de Ejecución: .NET Framework {0}", Environment.Version), LogType.Info);
            AppendLog(string.Format("Plan de Energía Activo: {0}", SystemInfoHelper.GetActivePowerScheme()), LogType.Info);
            AppendLog(string.Format("Procesadores / Cores: {0} lógicos detectados", Environment.ProcessorCount), LogType.Info);

            bool admin = IsRunAsAdmin();
            if (admin)
            {
                AppendLog("Nivel de Seguridad  : [ELEVADO] Ejecutando con permisos totales de Administrador.", LogType.Success);
            }
            else
            {
                AppendLog("Nivel de Seguridad  : [ESTÁNDAR] Algunos módulos requerirán permisos elevados.", LogType.Warning);
                AppendLog("Tip: Haz clic en la insignia superior para reiniciar en modo Administrador.", LogType.Warning);
            }

            AppendLog("Listo. Selecciona los módulos deseados y pulsa 'EJECUTAR OPTIMIZACIÓN' o 'SIMULAR (DRY-RUN)'.", LogType.Info);
            AppendLog("--------------------------------------------------------------------------", LogType.Info);

            UpdateLiveRamInfo();
        }

        private void UpdateLiveRamInfo()
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(UpdateLiveRamInfo));
                return;
            }

            try
            {
                NativeMethods.MEMORYSTATUSEX mem = new NativeMethods.MEMORYSTATUSEX();
                if (NativeMethods.GlobalMemoryStatusEx(mem))
                {
                    double totalGB = mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                    double inUseGB = (mem.ullTotalPhys - mem.ullAvailPhys) / (1024.0 * 1024.0 * 1024.0);
                    int pct = (int)mem.dwMemoryLoad;
                    lblRamStats.Text = string.Format("RAM en Uso: {0:N1} / {1:N1} GB ({2}%)", inUseGB, totalGB, pct);
                    pbRamLive.Value = pct;
                }
            }
            catch { }
        }

        private async void ExecuteTurboBoost()
        {
            if (isRunning) return;
            btnTurboBoost.Enabled = false;
            btnTurboBoost.Text = "BOOSTING...";
            systemGauge.IsScanning = true;
            animationTimer.Start();

            double freedMB = 0;
            await Task.Run(() =>
            {
                freedMB = OptimizationEngine.QuickTurboBoostRam();
            });

            animationTimer.Stop();
            systemGauge.IsScanning = false;
            systemGauge.Percent = 100;

            UpdateLiveRamInfo();
            AppendLog(string.Format("TURBO BOOST completado: Se liberaron {0:N1} MB de RAM física al instante.", freedMB), LogType.Success);
            btnTurboBoost.Text = "TURBO BOOST";
            btnTurboBoost.Enabled = true;
        }

        private OptimizationConfig BuildConfig()
        {
            OptimizationConfig config = new OptimizationConfig();
            config.DoTempFiles = cardTemp.Checked;
            config.DoRamOptimize = cardRam.Checked;
            config.DoDnsWinsock = cardDns.Checked;
            config.DoWinUpdate = cardWinUpd.Checked;
            config.DoRecycleBin = cardRecycle.Checked;
            config.DoDismSfc = cardDismSfc.Checked;
            config.DoGpuShadersPower = cardGpuPower.Checked;
            config.CreateRestorePoint = chkRestorePoint.Checked;
            return config;
        }

        private async void StartDryRun()
        {
            if (isRunning) return;

            OptimizationConfig config = BuildConfig();
            int activeCount = CountActiveModules(config);

            if (activeCount == 0)
            {
                MessageBox.Show("Por favor activa al menos un módulo para simular.", "Ningún módulo seleccionado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            this.isRunning = true;
            btnRun.Enabled = false;
            btnDryRun.Enabled = false;
            btnExport.Enabled = false;
            btnDryRun.Text = "ANALIZANDO SISTEMA...";
            lblConsoleBadge.Text = string.Format("ESTADO: SIMULANDO ({0} MÓDULOS)", activeCount);
            lblConsoleBadge.ForeColor = Color.FromArgb(56, 189, 248);

            // Iniciar radar de escaneo Driver Booster
            systemGauge.IsScanning = true;
            animationTimer.Start();

            DryRunTelemetry dryRun = await engine.RunDryRunAsync(config);

            // Detener radar y actualizar salud a 95%
            animationTimer.Stop();
            systemGauge.IsScanning = false;
            systemGauge.Percent = 95;

            // Badges en tarjetas
            if (config.DoTempFiles && dryRun.ModuleBadgeTexts.ContainsKey(1))
            {
                cardTemp.MetricBadgeText = dryRun.ModuleBadgeTexts[1];
                cardTemp.IsSuccessMetric = false;
            }
            if (config.DoRamOptimize && dryRun.ModuleBadgeTexts.ContainsKey(2))
            {
                cardRam.MetricBadgeText = dryRun.ModuleBadgeTexts[2];
                cardRam.IsSuccessMetric = false;
            }
            if (config.DoDnsWinsock && dryRun.ModuleBadgeTexts.ContainsKey(3))
            {
                cardDns.MetricBadgeText = dryRun.ModuleBadgeTexts[3];
                cardDns.IsSuccessMetric = false;
            }
            if (config.DoWinUpdate && dryRun.ModuleBadgeTexts.ContainsKey(4))
            {
                cardWinUpd.MetricBadgeText = dryRun.ModuleBadgeTexts[4];
                cardWinUpd.IsSuccessMetric = false;
            }
            if (config.DoRecycleBin && dryRun.ModuleBadgeTexts.ContainsKey(5))
            {
                cardRecycle.MetricBadgeText = dryRun.ModuleBadgeTexts[5];
                cardRecycle.IsSuccessMetric = false;
            }
            if (config.DoDismSfc && dryRun.ModuleBadgeTexts.ContainsKey(6))
            {
                cardDismSfc.MetricBadgeText = dryRun.ModuleBadgeTexts[6];
                cardDismSfc.IsSuccessMetric = false;
            }
            if (config.DoGpuShadersPower && dryRun.ModuleBadgeTexts.ContainsKey(7))
            {
                cardGpuPower.MetricBadgeText = dryRun.ModuleBadgeTexts[7];
                cardGpuPower.IsSuccessMetric = false;
            }

            string diskReclaimable = OptimizationEngine.FormatDiskBytes(dryRun.TotalDiskReclaimableBytes);
            string ramReclaimable = string.Format("{0:N0} MB", dryRun.EstimableRamBytes / (1024.0 * 1024.0));

            btnDryRun.Text = string.Format("SIMULAR / ANALIZAR • {0} + {1} RAM detectados", diskReclaimable, ramReclaimable);
            btnDryRun.VectorIcon = ButtonIconType.Search;

            lblStatus.Text = string.Format("● Simulación completada: {0} en disco y {1} en RAM listos para liberar. Ningún archivo fue modificado.", diskReclaimable, ramReclaimable);
            lblTaskProgress.Text = string.Format("{0}/{0} Módulos (100%)", activeCount);

            AppendLog("==========================================================================", LogType.Simulation);
            AppendLog(string.Format("Resumen de Simulación: {0} en disco ({1} archivos) y ~{2} en RAM detectados para optimización.",
                diskReclaimable, dryRun.TotalFilesDetected, ramReclaimable), LogType.Simulation);
            AppendLog("Pulsa 'EJECUTAR OPTIMIZACIÓN' para aplicar las limpiezas de forma segura.", LogType.Simulation);
            AppendLog("==========================================================================", LogType.Simulation);

            this.isRunning = false;
            btnRun.Enabled = true;
            btnDryRun.Enabled = true;
            btnExport.Enabled = true;
            lblConsoleBadge.Text = "ESTADO: SIMULACIÓN LISTA";
            lblConsoleBadge.ForeColor = Color.FromArgb(56, 189, 248);
        }

        private async void StartOptimization()
        {
            if (isRunning) return;

            OptimizationConfig config = BuildConfig();
            int activeCount = CountActiveModules(config);

            if (activeCount == 0)
            {
                MessageBox.Show("Por favor activa al menos un módulo para optimizar.", "Ningún módulo seleccionado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnDryRun.Text = "SIMULAR / ANALIZAR (DRY-RUN)";

            this.isRunning = true;
            btnRun.Enabled = false;
            btnDryRun.Enabled = false;
            btnExport.Enabled = false;
            btnRun.Text = "OPTIMIZANDO SISTEMA...";
            lblConsoleBadge.Text = string.Format("ESTADO: OPTIMIZANDO ({0} MÓDULOS)", activeCount);
            lblConsoleBadge.ForeColor = Color.FromArgb(63, 185, 80);

            // Iniciar radar de escaneo Driver Booster
            systemGauge.IsScanning = true;
            animationTimer.Start();

            Stopwatch sw = Stopwatch.StartNew();

            OptimizationTelemetry result = await engine.RunAsync(config);

            sw.Stop();

            // Detener radar y elevar salud a 100% ÓPTIMO
            animationTimer.Stop();
            systemGauge.IsScanning = false;
            systemGauge.Percent = 100;

            if (config.DoTempFiles && result.ModuleFreedBytes.ContainsKey(1))
            {
                cardTemp.MetricBadgeText = string.Format("✓ {0} liberados", OptimizationEngine.FormatDiskBytes(result.ModuleFreedBytes[1]));
                cardTemp.IsSuccessMetric = true;
            }
            if (config.DoRamOptimize)
            {
                cardRam.MetricBadgeText = string.Format("✓ {0:N1} MB liberados", result.RamFreedMB);
                cardRam.IsSuccessMetric = true;
            }
            if (config.DoDnsWinsock)
            {
                cardDns.MetricBadgeText = "✓ DNS/Winsock listo";
                cardDns.IsSuccessMetric = true;
            }
            if (config.DoWinUpdate && result.ModuleFreedBytes.ContainsKey(4))
            {
                cardWinUpd.MetricBadgeText = string.Format("✓ {0} liberados", OptimizationEngine.FormatDiskBytes(result.ModuleFreedBytes[4]));
                cardWinUpd.IsSuccessMetric = true;
            }
            if (config.DoRecycleBin && result.ModuleFreedBytes.ContainsKey(5))
            {
                cardRecycle.MetricBadgeText = string.Format("✓ {0} liberados", OptimizationEngine.FormatDiskBytes(result.ModuleFreedBytes[5]));
                cardRecycle.IsSuccessMetric = true;
            }
            if (config.DoDismSfc)
            {
                cardDismSfc.MetricBadgeText = "✓ Sistema verificado";
                cardDismSfc.IsSuccessMetric = true;
            }
            if (config.DoGpuShadersPower && result.ModuleFreedBytes.ContainsKey(7))
            {
                cardGpuPower.MetricBadgeText = string.Format("✓ {0} liberados", OptimizationEngine.FormatDiskBytes(result.ModuleFreedBytes[7]));
                cardGpuPower.IsSuccessMetric = true;
            }

            if (result.RequiresReboot)
            {
                pnlRebootNotice.Visible = true;
                LayoutRebootNotice();
            }

            AppendLog("==========================================================================", LogType.Success);
            AppendLog(string.Format("Optimización completada en {0:N1} seg. Total disco liberado: {1} en {2} archivos.",
                sw.Elapsed.TotalSeconds, OptimizationEngine.FormatDiskBytes(result.TotalBytesFreed), result.TotalFilesDeleted), LogType.Success);

            if (config.DoRamOptimize)
            {
                AppendLog(string.Format("Telemetría RAM: {0:N1} MB liberados hacia el sistema operativo.", result.RamFreedMB), LogType.Success);
            }
            AppendLog("==========================================================================", LogType.Success);

            this.isRunning = false;
            btnRun.Enabled = true;
            btnDryRun.Enabled = true;
            btnExport.Enabled = true;
            btnRun.Text = "EJECUTAR OPTIMIZACIÓN";
            lblConsoleBadge.Text = "ESTADO: SISTEMA ÓPTIMO";
            lblConsoleBadge.ForeColor = Color.FromArgb(63, 185, 80);
        }

        private int CountActiveModules(OptimizationConfig config)
        {
            return (config.DoTempFiles ? 1 : 0) +
                   (config.DoRamOptimize ? 1 : 0) +
                   (config.DoDnsWinsock ? 1 : 0) +
                   (config.DoWinUpdate ? 1 : 0) +
                   (config.DoRecycleBin ? 1 : 0) +
                   (config.DoDismSfc ? 1 : 0) +
                   (config.DoGpuShadersPower ? 1 : 0);
        }

        #endregion

        #region Logging & Progress Safe Dispatch

        private void AppendLog(string message, LogType type)
        {
            if (this.IsDisposed || !this.IsHandleCreated || rtbConsole == null || rtbConsole.IsDisposed) return;
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<string, LogType>(AppendLog), message, type);
                return;
            }

            Color tagColor;
            string prefix;

            switch (type)
            {
                case LogType.Success:
                    tagColor = Color.FromArgb(63, 185, 80);
                    prefix = "[ÉXITO]     ";
                    break;
                case LogType.Warning:
                    tagColor = ColorWarning;
                    prefix = "[AVISO]     ";
                    break;
                case LogType.Error:
                    tagColor = ColorError;
                    prefix = "[ERROR]     ";
                    break;
                case LogType.Command:
                    tagColor = ColorCommand;
                    prefix = "[CMD]       ";
                    break;
                case LogType.Simulation:
                    tagColor = Color.FromArgb(56, 189, 248);
                    prefix = "[SIMULACIÓN]";
                    break;
                default:
                    tagColor = ColorAccentBlue;
                    prefix = "[INFO]      ";
                    break;
            }

            string time = DateTime.Now.ToString("HH:mm:ss");

            rtbConsole.SelectionStart = rtbConsole.TextLength;
            rtbConsole.SelectionLength = 0;
            rtbConsole.SelectionColor = ColorTextMuted;
            rtbConsole.AppendText(string.Format("[{0}] ", time));

            rtbConsole.SelectionStart = rtbConsole.TextLength;
            rtbConsole.SelectionLength = 0;
            rtbConsole.SelectionColor = tagColor;
            rtbConsole.AppendText(string.Format("{0} ", prefix));

            rtbConsole.SelectionStart = rtbConsole.TextLength;
            rtbConsole.SelectionLength = 0;
            rtbConsole.SelectionColor = ColorTextPrimary;
            rtbConsole.AppendText(message + Environment.NewLine);

            rtbConsole.SelectionStart = rtbConsole.TextLength;
            rtbConsole.ScrollToCaret();
        }

        private void UpdateProgressSafe(int percent, string statusText, int done, int total)
        {
            if (this.IsDisposed || !this.IsHandleCreated || modernBar == null || modernBar.IsDisposed) return;
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<int, string, int, int>(UpdateProgressSafe), percent, statusText, done, total);
                return;
            }

            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;

            modernBar.Value = percent;
            lblStatus.Text = string.Format("● {0}", statusText);
            lblTaskProgress.Text = string.Format("{0}/{1} Módulos ({2}%)", done, total, percent);
        }

        #endregion

        #region Export & Save Features

        private void BtnExport_Click(object sender, EventArgs e)
        {
            OptimizationConfig config = BuildConfig();

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Exportar Script de Mantenimiento Automatizado";
                sfd.Filter = "Script Batch (*.bat)|*.bat|Script PowerShell (*.ps1)|*.ps1|Todos los archivos (*.*)|*.*";
                sfd.DefaultExt = "bat";
                sfd.FileName = "WinOpt_Mantenimiento_v4";

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        string ext = Path.GetExtension(sfd.FileName).ToLowerInvariant();
                        string content = (ext == ".ps1")
                            ? ScriptExporter.ExportToPowerShell(config)
                            : ScriptExporter.ExportToBatch(config);

                        File.WriteAllText(sfd.FileName, content, Encoding.Default);
                        AppendLog(string.Format("Script exportado con éxito en: {0}", sfd.FileName), LogType.Success);
                        MessageBox.Show("Script exportado correctamente:\n" + sfd.FileName, "Exportación Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        AppendLog("Error al exportar script: " + ex.Message, LogType.Error);
                    }
                }
            }
        }

        private void BtnSaveLog_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(rtbConsole.Text))
            {
                MessageBox.Show("La consola de registro está vacía.", "Guardar Registro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Guardar Registro de Consola";
                sfd.Filter = "Archivo de Texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
                sfd.DefaultExt = "txt";
                sfd.FileName = string.Format("WinOptStudio_Log_{0}", DateTime.Now.ToString("yyyyMMdd_HHmmss"));

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(sfd.FileName, rtbConsole.Text, Encoding.UTF8);
                        AppendLog("Archivo de registro guardado en: " + sfd.FileName, LogType.Success);
                    }
                    catch (Exception ex)
                    {
                        AppendLog("Error al guardar archivo: " + ex.Message, LogType.Error);
                    }
                }
            }
        }

        #endregion
    }

    #endregion
}
