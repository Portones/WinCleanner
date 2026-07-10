using System;
using System.Diagnostics;
using System.IO;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class ScheduledMaintenanceService : IScheduledMaintenanceService
    {
        private const string TaskName = "WinCleaner_Maintenance";

        public bool IsTaskEnabled()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/query /tn \"{TaskName}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(startInfo))
                {
                    process?.WaitForExit();
                    return process?.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error al comprobar el estado de la tarea programada.");
                return false;
            }
        }

        public string GetTaskNextRunTime()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/query /tn \"{TaskName}\" /fo list /v",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        using (var reader = new StringReader(output))
                        {
                            string? line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (line.Contains("Hora de próxima ejecución", StringComparison.OrdinalIgnoreCase) || 
                                    line.Contains("Next Run Time", StringComparison.OrdinalIgnoreCase))
                                {
                                    var parts = line.Split(':', 2);
                                    if (parts.Length == 2)
                                    {
                                        return parts[1].Trim();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error al obtener la próxima hora de ejecución de la tarea.");
            }
            return "No disponible";
        }

        public void EnableMaintenanceTask(string frequency, string dayOrMonthValue, string time)
        {
            string exePath = Environment.ProcessPath ?? throw new InvalidOperationException("No se pudo obtener el path de la aplicación.");

            string scheduleArgs = "";
            if (frequency.Equals("weekly", StringComparison.OrdinalIgnoreCase))
            {
                scheduleArgs = $"/sc weekly /d {dayOrMonthValue} /st {time}";
            }
            else if (frequency.Equals("monthly", StringComparison.OrdinalIgnoreCase))
            {
                scheduleArgs = $"/sc monthly /d {dayOrMonthValue} /st {time}";
            }
            else if (frequency.Equals("onlogon", StringComparison.OrdinalIgnoreCase))
            {
                scheduleArgs = "/sc onlogon";
            }
            else // idle
            {
                scheduleArgs = "/sc onidle /i 15";
            }

            // Crear tarea con privilegios elevados para evitar bloqueos
            string arguments = $"/create /tn \"{TaskName}\" /tr \"\\\"{exePath}\\\" --silent-clean\" {scheduleArgs} /rl HIGHEST /f";

            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = Process.Start(startInfo))
            {
                process?.WaitForExit();
                if (process?.ExitCode != 0)
                {
                    string error = process?.StandardError.ReadToEnd() ?? "Desconocido";
                    throw new Exception($"schtasks falló con código {process?.ExitCode}: {error}");
                }
            }
        }

        public void DisableMaintenanceTask()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/delete /tn \"{TaskName}\" /f",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = Process.Start(startInfo))
            {
                process?.WaitForExit();
            }
        }
    }
}
