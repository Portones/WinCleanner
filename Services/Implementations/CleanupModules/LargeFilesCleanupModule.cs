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
    public class LargeFilesCleanupModule : ICleanupModule
    {
        private readonly IConfigurationService _configurationService;

        public string Id => "LargeFiles";
        public string Name => "Archivos Grandes";
        public string Description => "Localiza archivos individuales pesados en directorios del usuario para liberar espacio rápidamente.";

        public LargeFilesCleanupModule(IConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        public async Task<ScanResult> ScanAsync(string selectedDrive, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var result = new ScanResult();
            var targetFolders = GetTargetFolders();
            var exDirs = _configurationService.CurrentSettings.ExcludedDirectories;
            long minSizeLimit = _configurationService.CurrentSettings.MinLargeFileSizeMb * 1024 * 1024; // MB a bytes

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
                    await Task.Run(() =>
                    {
                        foreach (var fileInfo in SafeDirectoryEnumerator.EnumerateFilesSafe(folder, exDirs))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            try
                            {
                                if (fileInfo.Exists && fileInfo.Length >= minSizeLimit)
                                {
                                    var item = new CleanableItem
                                    {
                                        Path = fileInfo.FullName,
                                        Name = fileInfo.Name,
                                        Size = fileInfo.Length,
                                        LastModified = fileInfo.LastWriteTime,
                                        FileType = "Archivo Grande",
                                        ModuleId = Id
                                    };
                                    lock (result)
                                    {
                                        result.Items.Add(item);
                                        result.TotalSize += item.Size;
                                    }
                                }
                            }
                            catch { }
                        }
                    }, cancellationToken);
                }
                catch (UnauthorizedAccessException) { }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al buscar archivos grandes en {Path}", folder);
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
                catch (Exception ex)
                {
                    Log.Warning("No se pudo eliminar el archivo grande {Path}: {Msg}", item.Path, ex.Message);
                }

                progress.Report((double)i / itemsToClean.Count * 100);
            }

            progress.Report(100);
            return cleanedCount;
        }

        private List<string> GetTargetFolders()
        {
            var list = new List<string>();
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var folders = new[] { "Downloads", "Documents", "Desktop", "Videos", "Music", "Pictures" };
            foreach (var folder in folders)
            {
                var fullPath = Path.Combine(userProfile, folder);
                if (Directory.Exists(fullPath))
                {
                    list.Add(fullPath);
                }
            }

            foreach (var dir in _configurationService.CurrentSettings.CustomScanDirectories)
            {
                if (Directory.Exists(dir) && !list.Contains(dir))
                {
                    list.Add(dir);
                }
            }

            return list;
        }
    }
}
