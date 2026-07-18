using System;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class SystemRestoreService : ISystemRestoreService
    {
        public async Task<int> GetSystemRestorePointCountAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    int count = 0;
                    using (var searcher = new ManagementObjectSearcher(@"root\default", "SELECT SequenceNumber FROM SystemRestore"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            count++;
                        }
                    }
                    return count;
                }
                catch (Exception ex)
                {
                    Log.Verbose(ex, "WMI SystemRestore falló o no está activo en esta edición de Windows.");
                    return 0;
                }
            });
        }

        public async Task<bool> DeleteOldRestorePointsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Eliminar copias de sombra antiguas dejando las más recientes mediante vssadmin
                    var psi = new ProcessStartInfo
                    {
                        FileName = "vssadmin.exe",
                        Arguments = "delete shadows /for=C: /oldest /quiet",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };

                    using var proc = Process.Start(psi);
                    proc?.WaitForExit();
                    return proc?.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al eliminar puntos de restauración antiguos.");
                    return false;
                }
            });
        }
    }
}
