using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class NetworkDiagnosticService : INetworkDiagnosticService
    {
        public async Task<long> MeasureLatencyAsync()
        {
            return await Task.Run(() =>
            {
                string[] targets = { "1.1.1.1", "8.8.8.8" };
                using var ping = new Ping();

                foreach (var target in targets)
                {
                    try
                    {
                        var reply = ping.Send(target, 1500);
                        if (reply.Status == IPStatus.Success)
                        {
                            return reply.RoundtripTime;
                        }
                    }
                    catch { }
                }

                return -1; // No conectado o sin respuesta
            });
        }

        public async Task<bool> FlushDnsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "ipconfig.exe",
                        Arguments = "/flushdns",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };

                    using var proc = Process.Start(psi);
                    proc?.WaitForExit();
                    return proc?.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al vaciar la caché DNS mediante ipconfig.");
                    return false;
                }
            });
        }
    }
}
