using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class SsdOptimizerService : ISsdOptimizerService
    {
        public async Task<List<DriveMediumInfo>> GetStorageDrivesAsync()
        {
            return await Task.Run(() =>
            {
                var drivesList = new List<DriveMediumInfo>();

                try
                {
                    var driveInfos = DriveInfo.GetDrives();
                    foreach (var d in driveInfos)
                    {
                        if (!d.IsReady || d.DriveType != System.IO.DriveType.Fixed) continue;

                        string letter = d.Name.TrimEnd('\\');
                        bool isSsd = CheckIfSsdDrive(letter);

                        drivesList.Add(new DriveMediumInfo
                        {
                            DriveLetter = letter,
                            VolumeLabel = string.IsNullOrEmpty(d.VolumeLabel) ? "Disco Local" : d.VolumeLabel,
                            TotalSize = d.TotalSize,
                            FreeSpace = d.AvailableFreeSpace,
                            IsSsd = isSsd,
                            DriveType = isSsd ? "Unidad SSD" : "Disco Duro (HDD)",
                            Status = isSsd ? "Listo para TRIM" : "No recomendado TRIM (HDD)"
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al obtener información de unidades de almacenamiento.");
                }

                return drivesList;
            });
        }

        public async Task<bool> OptimizeDriveAsync(DriveMediumInfo drive, IProgress<string> outputProgress, CancellationToken cancellationToken)
        {
            outputProgress.Report($"=== Iniciando optimización TRIM en la unidad {drive.DriveLetter} ===");
            Log.Information("Iniciando comando TRIM para {DriveLetter}", drive.DriveLetter);

            drive.Status = "Optimizando (TRIM)...";

            return await Task.Run(() =>
            {
                try
                {
                    string letterOnly = drive.DriveLetter.Replace(":", "").Trim();
                    string script = $"Optimize-Volume -DriveLetter {letterOnly} -ReTrim -Verbose";

                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };

                    using var process = new Process { StartInfo = psi };

                    DataReceivedEventHandler handler = (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            outputProgress.Report(e.Data);
                        }
                    };

                    process.OutputDataReceived += handler;
                    process.ErrorDataReceived += handler;

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    using (cancellationToken.Register(() =>
                    {
                        try
                        {
                            if (!process.HasExited)
                            {
                                process.Kill(true);
                                outputProgress.Report("\n[!] Operación de optimización cancelada.");
                            }
                        }
                        catch { }
                    }))
                    {
                        process.WaitForExit();
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    bool success = process.ExitCode == 0;
                    if (success)
                    {
                        drive.Status = "✅ Optimización TRIM completada";
                        outputProgress.Report($"\n[✅ ÉXITO] La unidad {drive.DriveLetter} fue reoptimizada correctamente.");
                        Log.Information("TRIM completado para {DriveLetter}", drive.DriveLetter);
                    }
                    else
                    {
                        drive.Status = "❌ Fallo en TRIM";
                        outputProgress.Report($"\n[❌ ERROR] La optimización de {drive.DriveLetter} finalizó con código de salida: {process.ExitCode}");
                        Log.Warning("Fallo al ejecutar TRIM en {DriveLetter}. ExitCode: {Code}", drive.DriveLetter, process.ExitCode);
                    }

                    return success;
                }
                catch (OperationCanceledException)
                {
                    drive.Status = "Cancelado";
                    outputProgress.Report("\n[!] Operación TRIM cancelada.");
                    return false;
                }
                catch (Exception ex)
                {
                    drive.Status = "Fallo";
                    outputProgress.Report($"\n[ERROR] Ocurrió un fallo al optimizar {drive.DriveLetter}: {ex.Message}");
                    Log.Error(ex, "Error al optimizar SSD {DriveLetter}", drive.DriveLetter);
                    return false;
                }
            }, cancellationToken);
        }

        private bool CheckIfSsdDrive(string driveLetter)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\Microsoft\Windows\Storage", "SELECT * FROM MSFT_PhysicalDisk");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var mediaType = obj["MediaType"];
                    if (mediaType != null)
                    {
                        ushort type = Convert.ToUInt16(mediaType);
                        if (type == 4) return true; // 4 = SSD
                    }
                }
            }
            catch
            {
                // Fallback WMI estándar Win32_DiskDrive
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var model = obj["Model"]?.ToString() ?? string.Empty;
                        var pType = obj["InterfaceType"]?.ToString() ?? string.Empty;
                        if (model.Contains("SSD", StringComparison.OrdinalIgnoreCase) || pType.Contains("NVMe", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                catch { }
            }

            // Por defecto en sistemas modernos se asume SSD si no es extraíble
            return true;
        }
    }
}
