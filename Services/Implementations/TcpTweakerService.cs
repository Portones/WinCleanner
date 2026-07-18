using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class TcpTweakerService : ITcpTweakerService
    {
        private const string TcpInterfacesKeyPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";

        public async Task<bool> IsTcpAutoTuningEnabledAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    string output = RunNetshCommand("int tcp show global");
                    if (string.IsNullOrEmpty(output)) return false;

                    // Buscar nivel de ajuste automático (normal vs disabled)
                    if (output.Contains("normal", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al consultar estado de TCP Auto-Tuning.");
                    return false;
                }
            });
        }

        public async Task<bool> SetTcpAutoTuningAsync(bool enable)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string level = enable ? "normal" : "disabled";
                    string output = RunNetshCommand($"int tcp set global autotuninglevel={level}");
                    Log.Information("TCP Auto-Tuning configurado a {Level}.", level);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al modificar TCP Auto-Tuning.");
                    return false;
                }
            });
        }

        public async Task<bool> IsNagleAlgorithmDisabledAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var baseKey = Registry.LocalMachine.OpenSubKey(TcpInterfacesKeyPath);
                    if (baseKey == null) return false;

                    foreach (var subKeyName in baseKey.GetSubKeyNames())
                    {
                        using var interfaceKey = baseKey.OpenSubKey(subKeyName);
                        if (interfaceKey == null) continue;

                        var ackFreq = interfaceKey.GetValue("TcpAckFrequency");
                        var noDelay = interfaceKey.GetValue("TCPNoDelay");

                        if (ackFreq is int ackVal && ackVal == 1 && noDelay is int delayVal && delayVal == 1)
                        {
                            return true;
                        }
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al consultar estado del Algoritmo de Nagle en el registro.");
                    return false;
                }
            });
        }

        public async Task<bool> SetNagleAlgorithmDisabledAsync(bool disableNagle)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var baseKey = Registry.LocalMachine.OpenSubKey(TcpInterfacesKeyPath, true);
                    if (baseKey == null) return false;

                    foreach (var subKeyName in baseKey.GetSubKeyNames())
                    {
                        using var interfaceKey = baseKey.OpenSubKey(subKeyName, true);
                        if (interfaceKey == null) continue;

                        if (disableNagle)
                        {
                            interfaceKey.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                            interfaceKey.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);
                        }
                        else
                        {
                            interfaceKey.DeleteValue("TcpAckFrequency", false);
                            interfaceKey.DeleteValue("TCPNoDelay", false);
                        }
                    }

                    Log.Information("Algoritmo de Nagle deshabilitado={DisableNagle} en todas las interfaces de red.", disableNagle);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al modificar la configuración del Algoritmo de Nagle en el registro.");
                    return false;
                }
            });
        }

        public async Task<bool> IsTcpChimneyOffloadEnabledAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    string output = RunNetshCommand("int tcp show global");
                    if (string.IsNullOrEmpty(output)) return false;

                    if (output.Contains("enabled", StringComparison.OrdinalIgnoreCase) ||
                        output.Contains("habilitado", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al consultar estado de TCP Chimney Offload.");
                    return false;
                }
            });
        }

        public async Task<bool> SetTcpChimneyOffloadAsync(bool enable)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string state = enable ? "enabled" : "disabled";
                    string output = RunNetshCommand($"int tcp set global chimney={state}");
                    Log.Information("TCP Chimney Offload configurado a {State}.", state);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al modificar TCP Chimney Offload.");
                    return false;
                }
            });
        }

        private string RunNetshCommand(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.GetEncoding(850)
            };

            using var process = Process.Start(psi);
            if (process == null) return string.Empty;

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }
    }
}
