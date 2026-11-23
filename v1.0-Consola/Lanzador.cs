using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

class Program
{
    static void Main()
    {
        // Extraer el .bat incrustado en una carpeta temporal segura
        string tempBat = Path.Combine(Path.GetTempPath(), "MantenimientoSistema_run.bat");
        
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MantenimientoSistema.bat"))
        using (FileStream fileStream = new FileStream(tempBat, FileMode.Create))
        {
            stream.CopyTo(fileStream);
        }

        // Ejecutar el .bat esperando a que termine y luego borrar el temporal
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = "cmd.exe";
        psi.Arguments = "/c \"" + tempBat + "\"";
        psi.UseShellExecute = true;
        psi.Verb = "runas"; // Solicita Administrador directamente desde el .exe

        try
        {
            Process p = Process.Start(psi);
            p.WaitForExit();
        }
        catch { }
        finally
        {
            if (File.Exists(tempBat)) File.Delete(tempBat);
        }
    }
}
