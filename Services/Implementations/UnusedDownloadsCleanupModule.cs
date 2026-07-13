using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class UnusedDownloadsCleanupModule : ICleanupModule
    {
        public string Id => "UnusedDownloads";
        public string Name => "Descargas Olvidadas";
        public string Description => "Archivos de más de 100 MB en la carpeta de Descargas que no han sido modificados en los últimos 6 meses.";

        public async Task<ScanResult> ScanAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var result = new ScanResult();
                progress.Report(10);

                try
                {
                    string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    if (Directory.Exists(downloadsPath))
                    {
                        var files = Directory.GetFiles(downloadsPath, "*.*", SearchOption.TopDirectoryOnly);
                        int totalFiles = files.Length;

                        for (int i = 0; i < totalFiles; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            string file = files[i];
                            var fileInfo = new FileInfo(file);

                            // Mayor de 100 MB y sin cambios en los últimos 180 días
                            if (fileInfo.Length > 100 * 1024 * 1024 && (DateTime.Now - fileInfo.LastWriteTime).TotalDays > 180)
                            {
                                result.Items.Add(new CleanableItem
                                {
                                    Path = file,
                                    Name = fileInfo.Name,
                                    Size = fileInfo.Length,
                                    LastModified = fileInfo.LastWriteTime,
                                    FileType = "Descarga Olvidada",
                                    ModuleId = Id
                                });
                                result.TotalSize += fileInfo.Length;
                            }

                            progress.Report(10 + ((double)i / Math.Max(1, totalFiles)) * 90);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al escanear descargas olvidadas.");
                }

                progress.Report(100);
                return result;
            });
        }

        public async Task<int> CleanAsync(List<CleanableItem> itemsToClean, IProgress<double> progress, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                int cleanedCount = 0;
                int totalItems = itemsToClean.Count;

                for (int i = 0; i < totalItems; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var item = itemsToClean[i];
                    try
                    {
                        if (File.Exists(item.Path))
                        {
                            File.Delete(item.Path);
                            cleanedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "No se pudo eliminar la descarga olvidada en {Path}", item.Path);
                    }

                    progress.Report(((double)i / Math.Max(1, totalItems)) * 100);
                }

                progress.Report(100);
                return cleanedCount;
            });
        }
    }
}
