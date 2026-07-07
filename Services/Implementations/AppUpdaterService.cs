using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class AppUpdaterService : IAppUpdaterService
    {
        public async Task<List<AppUpdateItem>> GetAvailableUpdatesAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var list = new List<AppUpdateItem>();
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = "upgrade",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8
                    };

                    using var process = new Process { StartInfo = startInfo };
                    process.Start();

                    // Leer salida estándar
                    using var reader = process.StandardOutput;
                    string? line;
                    bool separatorFound = false;

                    while ((line = reader.ReadLine()) != null)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // Buscar la línea de separación de columnas (ej. ----------)
                        if (!separatorFound)
                        {
                            if (line.StartsWith("---") || line.Contains("------"))
                            {
                                separatorFound = true;
                            }
                            continue;
                        }

                        // Ignorar líneas de resumen final
                        if (line.Contains("actualizaciones disponibles") || line.Contains("paquete(s) tienen números de versión"))
                        {
                            continue;
                        }

                        // Procesar línea de actualización
                        var parts = Regex.Split(line.Trim(), @"\s{2,}");
                        if (parts.Length >= 4)
                        {
                            list.Add(new AppUpdateItem
                            {
                                Name = parts[0],
                                Id = parts[1],
                                CurrentVersion = parts[2],
                                AvailableVersion = parts[3],
                                Source = parts.Length > 4 ? parts[4] : "winget",
                                IsSelected = true
                            });
                        }
                    }

                    process.WaitForExit(15000); // 15s de tiempo de espera máximo
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al obtener actualizaciones disponibles de Winget.");
                }
                return list;
            }, cancellationToken);
        }

        public async Task<bool> UpgradeAppAsync(AppUpdateItem app, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Log.Information("Iniciando actualización de {AppName} (ID: {AppId})", app.Name, app.Id);

                    // Argumentos silenciosos y de aceptación de contratos
                    string arguments = $"upgrade --id \"{app.Id}\" --silent --accept-package-agreements --accept-source-agreements";

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var process = new Process { StartInfo = startInfo };
                    process.Start();

                    process.WaitForExit(180000); // Esperar hasta 3 minutos por cada instalación

                    bool success = process.ExitCode == 0;
                    if (success)
                    {
                        Log.Information("Actualización de {AppName} finalizada con éxito.", app.Name);
                    }
                    else
                    {
                        Log.Warning("La actualización de {AppName} finalizó con código de salida: {ExitCode}", app.Name, process.ExitCode);
                    }

                    return success;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al ejecutar winget upgrade para {AppName}.", app.Name);
                    return false;
                }
            }, cancellationToken);
        }
    }
}
