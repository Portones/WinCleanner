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
    public class DevAndAppCacheCleanupModule : ICleanupModule
    {
        private readonly IConfigurationService _configurationService;

        public string Id => "DevAndAppCache";
        public string Name => "Caché de Desarrollo y Aplicaciones";
        public string Description => "Elimina archivos de caché generados por entornos de desarrollo (NuGet, .NET) y aplicaciones de usuario (Discord, Spotify).";

        public DevAndAppCacheCleanupModule(IConfigurationService configurationService)
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

                if (exDirs.Any(ex => string.Equals(ex.TrimEnd('\\'), folder.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)))
                {
                    processedFolders++;
                    continue;
                }

                try
                {
                    var files = await Task.Run(() => Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories), cancellationToken);

                    int fileIndex = 0;
                    foreach (var file in files)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

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
                                    FileType = "Caché Dev/App",
                                    ModuleId = Id
                                };
                                result.Items.Add(item);
                                result.TotalSize += item.Size;
                            }
                        }
                        catch (FileNotFoundException) { }
                        catch (UnauthorizedAccessException) { }
                        catch (IOException) { }

                        fileIndex++;
                        if (fileIndex % 50 == 0)
                        {
                            double folderProgress = (double)processedFolders / targetFolders.Count * 100;
                            double innerProgress = ((double)fileIndex / Math.Max(1, files.Length)) * (100.0 / targetFolders.Count);
                            progress.Report(folderProgress + innerProgress);
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log.Warning("Acceso no autorizado al escanear caché en {Path}: {Msg}", folder, ex.Message);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al escanear carpeta de caché Dev/App: {Path}", folder);
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
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
                catch (Exception ex)
                {
                    Log.Warning("No se pudo eliminar el archivo de caché {Path}: {Msg}", item.Path, ex.Message);
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
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var list = new List<string>();

            // Caché de paquetes NuGet
            if (!string.IsNullOrEmpty(userProfile))
            {
                list.Add(Path.Combine(userProfile, ".nuget", "packages"));
            }

            // Caché de Discord
            if (!string.IsNullOrEmpty(appData))
            {
                list.Add(Path.Combine(appData, "discord", "Cache"));
                list.Add(Path.Combine(appData, "discord", "Code Cache"));
                list.Add(Path.Combine(appData, "discord", "GPUCache"));
            }

            // Caché de Spotify
            if (!string.IsNullOrEmpty(localAppData))
            {
                list.Add(Path.Combine(localAppData, "Spotify", "Storage"));
                list.Add(Path.Combine(localAppData, "Temp", "NuGetScratch"));
            }

            return list;
        }
    }
}
