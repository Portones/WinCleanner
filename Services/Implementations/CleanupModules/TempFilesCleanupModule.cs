using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Helpers;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations.CleanupModules
{
    public class TempFilesCleanupModule : ICleanupModule
    {
        private readonly IConfigurationService _configurationService;

        public string Id => "TempFiles";
        public string Name => "Archivos Temporales";
        public string Description => "Elimina archivos de almacenamiento temporal de Windows, cachés de instalación y archivos de registro temporales.";

        public TempFilesCleanupModule(IConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        public async Task<ScanResult> ScanAsync(string selectedDrive, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var result = new ScanResult();
            var targetFolders = GetTargetFolders();
            var exDirs = _configurationService.CurrentSettings.ExcludedDirectories;

            int processedFolders = 0;
            progress.Report(0);

            string? driveFilter = (!string.IsNullOrEmpty(selectedDrive) && !selectedDrive.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                ? selectedDrive.TrimEnd('\\').ToLowerInvariant()
                : null;

            foreach (var folder in targetFolders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Directory.Exists(folder))
                {
                    processedFolders++;
                    continue;
                }

                if (driveFilter != null && !folder.ToLowerInvariant().StartsWith(driveFilter))
                {
                    processedFolders++;
                    continue;
                }

                // Verificar si está en la lista de excluidos
                if (exDirs.Any(ex => string.Equals(ex.TrimEnd('\\'), folder.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)))
                {
                    processedFolders++;
                    continue;
                }

                try
                {
                    // Buscar archivos de forma no bloqueante en el pool de hilos
                    var files = await Task.Run(() => Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories), cancellationToken);
                    
                    int fileIndex = 0;
                    foreach (var file in files)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Omitir si el archivo o su ruta está bajo alguna carpeta excluida
                        if (exDirs.Any(ex => file.StartsWith(ex, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        try
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.Exists)
                            {
                                var item = new CleanableItem
                                {
                                    Path = file,
                                    Name = fileInfo.Name,
                                    Size = fileInfo.Length,
                                    LastModified = fileInfo.LastWriteTime,
                                    FileType = "Archivo Temporal",
                                    ModuleId = Id
                                };
                                result.Items.Add(item);
                                result.TotalSize += item.Size;
                            }
                        }
                        catch (FileNotFoundException) { /* Archivo eliminado durante escaneo */ }
                        catch (UnauthorizedAccessException) { /* Sin permisos de acceso */ }
                        catch (IOException) { /* Archivo bloqueado por proceso activo */ }

                        fileIndex++;
                        if (fileIndex % 50 == 0) // Actualizar progreso espaciadamente para rendimiento
                        {
                            double folderProgress = (double)processedFolders / targetFolders.Count * 100;
                            double innerProgress = ((double)fileIndex / files.Length) * (100.0 / targetFolders.Count);
                            progress.Report(folderProgress + innerProgress);
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log.Warning("Acceso no autorizado al escanear temporales en {Path}: {Msg}", folder, ex.Message);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al escanear carpeta temporal: {Path}", folder);
                }

                processedFolders++;
                progress.Report((double)processedFolders / targetFolders.Count * 100);
            }

            progress.Report(100);
            return result;
        }

        public async Task<int> CleanAsync(List<CleanableItem> itemsToClean, IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (itemsToClean == null || itemsToClean.Count == 0) return 0;

            int cleanedCount = 0;
            bool bypassRecycle = _configurationService.CurrentSettings.BypassRecycleBin;

            for (int i = 0; i < itemsToClean.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = itemsToClean[i];

                try
                {
                    if (File.Exists(item.Path))
                    {
                        if (bypassRecycle)
                        {
                            File.Delete(item.Path);
                            cleanedCount++;
                        }
                        else
                        {
                            bool success = ShellFileOperations.SendToRecycleBin(item.Path);
                            if (success) cleanedCount++;
                        }
                    }
                }
                catch (UnauthorizedAccessException) { /* Archivo en uso */ }
                catch (IOException) { /* Archivo bloqueado */ }
                catch (Exception ex)
                {
                    Log.Warning("No se pudo eliminar el archivo temporal {Path}: {Msg}", item.Path, ex.Message);
                }

                if (i % 20 == 0 || i == itemsToClean.Count - 1)
                {
                    progress.Report((double)i / itemsToClean.Count * 100);
                }
            }

            progress.Report(100);
            return cleanedCount;
        }

        private List<string> GetTargetFolders()
        {
            var list = new List<string>
            {
                Path.GetTempPath(), // %TEMP% del usuario
                @"C:\Windows\Temp",
                @"C:\Windows\Prefetch",
                @"C:\Windows\SoftwareDistribution\Download", // Caché de Windows Update
                @"C:\Windows\Minidump",
                @"C:\Windows\Logs"
            };

            // Intentar agregar la caché de Delivery Optimization
            var windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var deliveryOptimization = Path.Combine(windir, @"ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache");
            list.Add(deliveryOptimization);

            return list;
        }
    }
}
