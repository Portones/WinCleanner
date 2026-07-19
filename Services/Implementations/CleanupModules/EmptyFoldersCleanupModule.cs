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
    public class EmptyFoldersCleanupModule : ICleanupModule
    {
        private readonly IConfigurationService _configurationService;

        public string Id => "EmptyFolders";
        public string Name => "Carpetas Vacías";
        public string Description => "Busca y elimina carpetas que no contienen archivos ni subcarpetas de forma recursiva, liberando desorden en el disco.";

        public EmptyFoldersCleanupModule(IConfigurationService configurationService)
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

            var foundFolders = new List<CleanableItem>();

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

                // Omitir si la carpeta principal está en la lista de excluidos
                if (exDirs.Any(ex => string.Equals(ex.TrimEnd('\\'), folder.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)))
                {
                    processedFolders++;
                    continue;
                }

                try
                {
                    await Task.Run(() => FindEmptyFolders(folder, foundFolders, exDirs, cancellationToken), cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al buscar carpetas vacías en {Path}", folder);
                }

                processedFolders++;
                progress.Report((double)processedFolders / targetFolders.Count * 100);
            }

            result.Items = foundFolders;
            result.TotalSize = 0; // Las carpetas vacías no tienen tamaño asociado

            progress.Report(100);
            return result;
        }

        public async Task<int> CleanAsync(List<CleanableItem> itemsToClean, IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (itemsToClean == null || itemsToClean.Count == 0) return 0;

            int cleanedCount = 0;
            bool bypassRecycle = _configurationService.CurrentSettings.BypassRecycleBin;

            // Ordenar carpetas de forma descendente por longitud de ruta 
            // (esto asegura que eliminamos primero las subcarpetas más profundas y luego los padres)
            var sortedItems = itemsToClean.OrderByDescending(x => x.Path.Length).ToList();

            for (int i = 0; i < sortedItems.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = sortedItems[i];

                try
                {
                    if (Directory.Exists(item.Path))
                    {
                        if (bypassRecycle)
                        {
                            Directory.Delete(item.Path, false); // No recursivo: solo borrar si está vacía
                            cleanedCount++;
                        }
                        else
                        {
                            bool success = ShellFileOperations.SendToRecycleBin(item.Path);
                            if (success) cleanedCount++;
                        }
                    }
                }
                catch (UnauthorizedAccessException) { /* Carpeta del sistema protegida */ }
                catch (IOException) { /* Carpeta ya eliminada o no vacía en ejecución */ }
                catch (Exception ex)
                {
                    Log.Warning("No se pudo eliminar la carpeta vacía {Path}: {Msg}", item.Path, ex.Message);
                }

                if (i % 10 == 0 || i == sortedItems.Count - 1)
                {
                    progress.Report((double)i / sortedItems.Count * 100);
                }
            }

            progress.Report(100);
            return cleanedCount;
        }

        private void FindEmptyFolders(string path, List<CleanableItem> foundItems, List<string> exDirs, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Verificar si la carpeta actual o su padre está en la lista negra
                if (exDirs.Any(ex => path.StartsWith(ex, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                // Obtener subdirectorios
                var subdirs = Directory.GetDirectories(path);
                foreach (var subdir in subdirs)
                {
                    FindEmptyFolders(subdir, foundItems, exDirs, cancellationToken);
                }

                // Comprobar archivos y carpetas actuales después de procesar subdirectorios
                var files = Directory.GetFiles(path);
                var currentSubdirs = Directory.GetDirectories(path);

                // Si no hay archivos y tampoco subcarpetas, está vacía
                if (files.Length == 0 && currentSubdirs.Length == 0)
                {
                    // Evitar incluir la carpeta raíz misma
                    if (GetTargetFolders().Any(target => string.Equals(target.TrimEnd('\\'), path.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)))
                    {
                        return;
                    }

                    var dirInfo = new DirectoryInfo(path);
                    foundItems.Add(new CleanableItem
                    {
                        Path = path,
                        Name = dirInfo.Name,
                        Size = 0,
                        LastModified = dirInfo.LastWriteTime,
                        FileType = "Carpeta Vacía",
                        ModuleId = Id
                    });
                }
            }
            catch (UnauthorizedAccessException) { /* Omitir directorios protegidos */ }
            catch (IOException) { /* Omitir bloqueados */ }
        }

        private List<string> GetTargetFolders()
        {
            var list = new List<string>();

            // Descargas del usuario
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var downloads = Path.Combine(userProfile, "Downloads");
            if (Directory.Exists(downloads))
            {
                list.Add(downloads);
            }

            // Temp del usuario
            list.Add(Path.GetTempPath());

            // Agregar directorios personalizados configurados por el usuario
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
