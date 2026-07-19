using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class RedundantInstallersCleanupModule : ICleanupModule
    {
        private readonly IAppUninstallerService _uninstallerService;

        public string Id => "RedundantInstallers";
        public string Name => "Instaladores Redundantes";
        public string Description => "Archivos de instalación (.exe, .msi) en Descargas y Escritorio de aplicaciones que ya tienes instaladas.";

        public RedundantInstallersCleanupModule(IAppUninstallerService uninstallerService)
        {
            _uninstallerService = uninstallerService ?? throw new ArgumentNullException(nameof(uninstallerService));
        }

        public async Task<ScanResult> ScanAsync(string selectedDrive, IProgress<double> progress, CancellationToken cancellationToken)
        {
            return await Task.Run(async () =>
            {
                var result = new ScanResult();
                progress.Report(10);

                string? driveFilter = (!string.IsNullOrEmpty(selectedDrive) && !selectedDrive.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                    ? selectedDrive.TrimEnd('\\').ToLowerInvariant()
                    : null;

                try
                {
                    // 1. Obtener la lista de aplicaciones instaladas
                    var installedApps = await _uninstallerService.GetInstalledAppsAsync(cancellationToken);
                    var installedNames = installedApps
                        .Select(a => a.DisplayName.ToLowerInvariant())
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToList();

                    progress.Report(30);

                    // 2. Buscar archivos de instalación en Descargas y Escritorio
                    string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                    var files = new List<string>();
                    if (Directory.Exists(downloadsPath) && (driveFilter == null || downloadsPath.ToLowerInvariant().StartsWith(driveFilter)))
                    {
                        files.AddRange(Directory.GetFiles(downloadsPath, "*.exe"));
                        files.AddRange(Directory.GetFiles(downloadsPath, "*.msi"));
                    }
                    if (Directory.Exists(desktopPath) && (driveFilter == null || desktopPath.ToLowerInvariant().StartsWith(driveFilter)))
                    {
                        files.AddRange(Directory.GetFiles(desktopPath, "*.exe"));
                        files.AddRange(Directory.GetFiles(desktopPath, "*.msi"));
                    }

                    progress.Report(50);

                    int totalFiles = files.Count;
                    for (int i = 0; i < totalFiles; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        string file = files[i];
                        string fileName = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();

                        // Buscar coincidencias e identificar si es un instalador/setup
                        if (fileName.Contains("setup") || fileName.Contains("install") || fileName.Contains("update"))
                        {
                            // Limpiar términos comunes de instalador para aislar el nombre de la app
                            string cleanName = fileName
                                .Replace("setup", "")
                                .Replace("install", "")
                                .Replace("update", "")
                                .Replace("x64", "")
                                .Replace("x86", "")
                                .Replace("win", "")
                                .Replace("user", "")
                                .Trim('-', '_', ' ');

                            if (cleanName.Length >= 3)
                            {
                                // Comprobar si hay una aplicación instalada cuyo nombre contenga el nombre limpio del instalador
                                bool isRedundant = installedNames.Any(app => app.Contains(cleanName));
                                if (isRedundant)
                                {
                                    var fileInfo = new FileInfo(file);
                                    result.Items.Add(new CleanableItem
                                    {
                                        Path = file,
                                        Name = fileInfo.Name,
                                        Size = fileInfo.Length,
                                        LastModified = fileInfo.LastWriteTime,
                                        FileType = "Instalador Redundante",
                                        ModuleId = Id
                                    });
                                    result.TotalSize += fileInfo.Length;
                                }
                            }
                        }

                        progress.Report(50 + ((double)i / Math.Max(1, totalFiles)) * 50);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al escanear instaladores redundantes.");
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
                        Log.Warning(ex, "No se pudo eliminar el instalador redundante en {Path}", item.Path);
                    }

                    progress.Report(((double)i / Math.Max(1, totalItems)) * 100);
                }

                progress.Report(100);
                return cleanedCount;
            });
        }
    }
}
