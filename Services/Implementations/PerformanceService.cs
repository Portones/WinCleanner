using System;
using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class PerformanceService : IPerformanceService
    {
        public async Task<long?> PingDnsServerAsync(string ipAddress, CancellationToken cancellationToken)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(ipAddress, 1200); // 1.2s timeout
                    if (reply.Status == IPStatus.Success)
                    {
                        return reply.RoundtripTime;
                    }
                }
            }
            catch { }
            return null;
        }

        public async Task<bool> ApplyDnsSettingsAsync(string primaryDns, string secondaryDns, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                bool success = false;
                try
                {
                    using (var mc = new ManagementClass("Win32_NetworkAdapterConfiguration"))
                    using (var moc = mc.GetInstances())
                    {
                        foreach (ManagementObject mo in moc)
                        {
                            if ((bool)mo["IPEnabled"])
                            {
                                using (var inParams = mo.GetMethodParameters("SetDNSServerSearchOrder"))
                                {
                                    inParams["DNSServerSearchOrder"] = new string[] { primaryDns, secondaryDns };
                                    using (var outParams = mo.InvokeMethod("SetDNSServerSearchOrder", inParams, null))
                                    {
                                        uint returnValue = (uint)outParams["ReturnValue"];
                                        if (returnValue == 0 || returnValue == 1) // 0 = Éxito, 1 = Requiere Reinicio (pero aplica igual)
                                        {
                                            success = true;
                                            Log.Information("DNS {Primary}/{Secondary} aplicado correctamente al adaptador WMI {Desc}.", primaryDns, secondaryDns, mo["Description"]);
                                        }
                                        else
                                        {
                                            Log.Warning("Error al aplicar DNS en adaptador, valor devuelto: {Val}", returnValue);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error crítico al reconfigurar DNS del adaptador por WMI.");
                }
                return success;
            });
        }

        public async Task<bool> FlushDnsCacheAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "ipconfig",
                        Arguments = "/flushdns",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (var process = Process.Start(psi))
                    {
                        if (process != null)
                        {
                            process.WaitForExit();
                            Log.Information("Caché DNS vaciada mediante ipconfig /flushdns (código de salida {Code}).", process.ExitCode);
                            return process.ExitCode == 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al vaciar caché DNS del sistema.");
                }
                return false;
            });
        }

        public async Task<string> GetActiveDnsAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var mc = new ManagementClass("Win32_NetworkAdapterConfiguration"))
                    using (var moc = mc.GetInstances())
                    {
                        foreach (ManagementObject mo in moc)
                        {
                            if ((bool)mo["IPEnabled"])
                            {
                                var dnsServers = mo["DNSServerSearchOrder"] as string[];
                                if (dnsServers != null && dnsServers.Length > 0)
                                {
                                    return string.Join(", ", dnsServers);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("No se pudieron leer las DNS activas mediante WMI: {Msg}", ex.Message);
                }
                return "Por DHCP / Automático";
            });
        }

        public bool GetTelemetryState()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", false))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("AllowTelemetry");
                        if (val != null && (int)val == 0)
                        {
                            return true; // Desactivado
                        }
                    }
                }
            }
            catch { }
            return false; // Activado o no configurado
        }

        public async Task SetTelemetryStateAsync(bool disableTelemetry)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", true))
                    {
                        if (disableTelemetry)
                        {
                            key.SetValue("AllowTelemetry", 0, RegistryValueKind.DWord);
                            Log.Information("Directiva AllowTelemetry desactivada (0) en registro HKLM.");
                        }
                        else
                        {
                            key.DeleteValue("AllowTelemetry", false);
                            Log.Information("Directiva AllowTelemetry restaurada (eliminada) del registro.");
                        }
                    }

                    // Detener/Deshabilitar servicio DiagTrack (Experiencias de telemetría)
                    ToggleService("DiagTrack", !disableTelemetry);
                    // Detener/Deshabilitar servicio de inserción WAP (dmwappushservice)
                    ToggleService("dmwappushservice", !disableTelemetry);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al configurar telemetría de Windows en Registro/Servicios.");
                }
            });
        }

        private void ToggleService(string serviceName, bool enable)
        {
            try
            {
                using (var sc = new ServiceController(serviceName))
                {
                    if (enable)
                    {
                        // Habilitar a Automático
                        using (var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}", true))
                        {
                            key?.SetValue("Start", 2, RegistryValueKind.DWord); // 2 = Automático
                        }
                        // Iniciar si está parado
                        if (sc.Status == ServiceControllerStatus.Stopped)
                        {
                            sc.Start();
                            Log.Information("Servicio {Name} iniciado correctamente.", serviceName);
                        }
                    }
                    else
                    {
                        // Parar si está corriendo
                        if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.StartPending)
                        {
                            sc.Stop();
                            Log.Information("Servicio {Name} detenido correctamente.", serviceName);
                        }
                        // Deshabilitar
                        using (var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}", true))
                        {
                            key?.SetValue("Start", 4, RegistryValueKind.DWord); // 4 = Deshabilitado
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("No se pudo alternar el servicio {Name}: {Msg}", serviceName, ex.Message);
            }
        }

        public bool GetErrorReportingState()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", false))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("Disabled");
                        if (val != null && (int)val == 1)
                        {
                            return true; // Desactivado
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public async Task SetErrorReportingStateAsync(bool disableErrorReporting)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", true))
                    {
                        if (disableErrorReporting)
                        {
                            key.SetValue("Disabled", 1, RegistryValueKind.DWord);
                            Log.Information("Directiva Windows Error Reporting configurada a Desactivado (1) en registro.");
                        }
                        else
                        {
                            key.DeleteValue("Disabled", false);
                            Log.Information("Directiva Windows Error Reporting restaurada (eliminada) del registro.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al configurar directiva de informes de error.");
                }
            });
        }

        public async Task<bool> SetGameModeStateAsync(bool enableGameMode)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (enableGameMode)
                    {
                        // Cambiar plan de energía a "Alto Rendimiento" (GUID estándar de Windows)
                        var psi = new ProcessStartInfo
                        {
                            FileName = "powercfg",
                            Arguments = "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using (var p = Process.Start(psi))
                        {
                            p?.WaitForExit();
                        }

                        // Parar Spooler (Cola de Impresión) y Bluetooth para liberar recursos
                        ToggleService("Spooler", false);
                        ToggleService("bthserv", false);

                        Log.Information("Modo Juego activado: Plan Alto Rendimiento y servicios no esenciales pausados.");
                        return true;
                    }
                    else
                    {
                        // Restaurar el plan a "Equilibrado" (GUID estándar de Windows: 381b4222-f694-41f0-9685-ff5bb260df2e)
                        var psi = new ProcessStartInfo
                        {
                            FileName = "powercfg",
                            Arguments = "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using (var p = Process.Start(psi))
                        {
                            p?.WaitForExit();
                        }

                        // Habilitar y arrancar cola de impresión y bluetooth
                        ToggleService("Spooler", true);
                        ToggleService("bthserv", true);

                        Log.Information("Modo Juego desactivado: Plan de energía restaurado a Equilibrado y servicios reiniciados.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al alternar estado de Modo Juego.");
                    return false;
                }
            });
        }

        public async Task<string> GetActivePowerSchemeAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powercfg",
                        Arguments = "/getactivescheme",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using (var proc = Process.Start(psi))
                    {
                        if (proc != null)
                        {
                            string output = proc.StandardOutput.ReadToEnd();
                            proc.WaitForExit();
                            var match = Regex.Match(output, @"\(([^)]+)\)");
                            if (match.Success)
                            {
                                return match.Groups[1].Value;
                            }
                        }
                    }
                }
                catch { }
                return "Equilibrado / Por defecto";
            });
        }

        public async Task<double> RunInternetSpeedTestAsync(CancellationToken cancellationToken)
        {
            // Descarga un bloque de 10 MB del CDN de Cloudflare para medir la velocidad real de bajada.
            const string speedTestUrl = "https://speed.cloudflare.com/__down?bytes=10000000";
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(20);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var bytes = await client.GetByteArrayAsync(speedTestUrl, cancellationToken);
                sw.Stop();
                if (bytes.Length == 0 || sw.Elapsed.TotalSeconds == 0) return 0;
                // Bytes → bits → Mbps
                double mbps = (bytes.Length * 8.0) / sw.Elapsed.TotalSeconds / 1_000_000.0;
                return Math.Round(mbps, 1);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Speed test de internet falló.");
                return -1;
            }
        }
    }
}
