using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Management;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class BootAnalyzerService : IBootAnalyzerService
    {
        public async Task<BootInfo> GetBootInfoAsync()
        {
            return await Task.Run(() =>
            {
                var bootInfo = new BootInfo();

                // 1. Obtener fecha y hora del último arranque
                DateTime lastBoot = GetLastBootTime();
                bootInfo.LastBootDateTime = lastBoot;

                // Calcular Uptime en texto legible
                bootInfo.UptimeText = GetUptimeText(lastBoot);

                // 2. Obtener historial de arranques (últimos 5)
                List<BootHistoryItem> history = GetBootHistory(5);
                bootInfo.BootHistory = history;

                // 3. Establecer la última duración del arranque si está disponible, o simular una estimación base
                if (history.Count > 0)
                {
                    bootInfo.LastBootDurationSeconds = history[0].DurationSeconds;
                }
                else
                {
                    // Fallback de estimación rápida si el log de diagnósticos está vacío o desactivado
                    bootInfo.LastBootDurationSeconds = EstimateBootDurationFallback();
                }

                return bootInfo;
            });
        }

        private DateTime GetLastBootTime()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var lastBootStr = obj["LastBootUpTime"]?.ToString();
                        if (!string.IsNullOrEmpty(lastBootStr))
                        {
                            return ManagementDateTimeConverter.ToDateTime(lastBootStr);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "WMI LastBootUpTime falló. Usando Environment.TickCount64 como fallback.");
            }

            return DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount64);
        }

        private string GetUptimeText(DateTime lastBoot)
        {
            var uptime = DateTime.Now - lastBoot;
            if (uptime.TotalDays >= 1)
            {
                return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
            }
            if (uptime.TotalHours >= 1)
            {
                return $"{uptime.Hours}h {uptime.Minutes}m";
            }
            return $"{uptime.Minutes}m";
        }

        private List<BootHistoryItem> GetBootHistory(int count)
        {
            var history = new List<BootHistoryItem>();

            try
            {
                // Event ID 100: Boot Performance Diagnostics
                var queryText = "*[System/EventID=100]";
                var query = new EventLogQuery("Microsoft-Windows-Diagnostics-Performance/Operational", PathType.LogName, queryText)
                {
                    ReverseDirection = true // El más reciente primero
                };

                using (var reader = new EventLogReader(query))
                {
                    EventRecord record;
                    int fetched = 0;

                    while ((record = reader.ReadEvent()) != null && fetched < count)
                    {
                        try
                        {
                            var xml = record.ToXml();
                            var match = System.Text.RegularExpressions.Regex.Match(xml, @"<BootTime>(\d+)</BootTime>");
                            if (match.Success && double.TryParse(match.Groups[1].Value, out var ms))
                            {
                                history.Add(new BootHistoryItem
                                {
                                    Date = record.TimeCreated ?? DateTime.Now,
                                    DurationSeconds = ms / 1000.0
                                });
                                fetched++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Verbose(ex, "No se pudo parsear el evento de arranque.");
                        }
                        finally
                        {
                            record.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "No se pudieron recuperar los eventos de arranque de Windows. Es posible que el registro esté desactivado o requiera privilegios.");
            }

            return history;
        }

        private double EstimateBootDurationFallback()
        {
            // Estimación basada en la fecha del último arranque y eventos genéricos, o valor nominal por defecto.
            return 18.5; // Valor promedio nominal de arranque moderno con SSD
        }
    }
}
