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
    public class BrowserCacheCleanupModule : ICleanupModule
    {
        private readonly IConfigurationService _configurationService;

        public string Id => "BrowserCache";
        public string Name => "Caché de Navegadores";
        public string Description => "Busca y cuantifica el espacio ocupado por los archivos de caché temporal de Chrome, Edge y Firefox.";

        public BrowserCacheCleanupModule(IConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        public async Task<ScanResult> ScanAsync(string selectedDrive, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var result = new ScanResult();
            progress.Report(0);

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            
            // Lista de rutas de perfiles de navegadores y sus nombres descriptivos
            var browserScans = new List<(string Path, string BrowserName, string CacheSubfolder)>
            {
                (Path.Combine(localAppData, @"Google\Chrome\User Data"), "Google Chrome", "Cache"),
                (Path.Combine(localAppData, @"Microsoft\Edge\User Data"), "Microsoft Edge", "Cache"),
                (Path.Combine(localAppData, @"Mozilla\Firefox\Profiles"), "Mozilla Firefox", "cache2")
            };

            int processedBrowsers = 0;

            string? driveFilter = (!string.IsNullOrEmpty(selectedDrive) && !selectedDrive.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                ? selectedDrive.TrimEnd('\\').ToLowerInvariant()
                : null;

            foreach (var scan in browserScans)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (driveFilter != null && !scan.Path.ToLowerInvariant().StartsWith(driveFilter))
                {
                    processedBrowsers++;
                    continue;
                }

                if (!Directory.Exists(scan.Path))
                {
                    processedBrowsers++;
                    continue;
                }

                try
                {
                    // Encontrar todos los perfiles de usuario en la ruta del navegador (Default, Profile 1, etc.)
                    var profileDirs = new List<string>();

                    if (scan.BrowserName == "Mozilla Firefox")
                    {
                        // En Firefox, la carpeta "Profiles" contiene directamente las carpetas de perfiles aleatorios
                        profileDirs.AddRange(Directory.GetDirectories(scan.Path));
                    }
                    else
                    {
                        // En Chrome/Edge, buscamos "Default" y carpetas que empiecen por "Profile "
                        var subdirs = Directory.GetDirectories(scan.Path);
                        foreach (var subdir in subdirs)
                        {
                            var name = Path.GetFileName(subdir);
                            if (name == "Default" || name.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase))
                            {
                                profileDirs.Add(subdir);
                            }
                        }
                    }

                    foreach (var profileDir in profileDirs)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var cacheDir = Path.Combine(profileDir, scan.CacheSubfolder);

                        if (Directory.Exists(cacheDir))
                        {
                            // Calcular el tamaño total de la caché en segundo plano
                            var folderSize = await Task.Run(() => GetFolderSize(cacheDir, cancellationToken), cancellationToken);

                            if (folderSize > 0)
                            {
                                var profileName = Path.GetFileName(profileDir);
                                var item = new CleanableItem
                                {
                                    Path = cacheDir,
                                    Name = $"Caché de {scan.BrowserName} ({profileName})",
                                    Size = folderSize,
                                    LastModified = DateTime.Now,
                                    FileType = "Caché de Navegador",
                                    ModuleId = Id
                                };
                                result.Items.Add(item);
                                result.TotalSize += folderSize;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al escanear la caché del navegador {Browser}", scan.BrowserName);
                }

                processedBrowsers++;
                progress.Report((double)processedBrowsers / browserScans.Count * 100);
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
                    if (Directory.Exists(item.Path))
                    {
                        // En la caché del navegador, borramos de forma recursiva todo el contenido de la carpeta.
                        // Como es caché temporal inútil, se borra directamente de forma permanente para evitar 
                        // llenar la papelera con miles de archivos pequeños (incluso si bypassRecycle es false, 
                        // es mejor borrar caché de forma directa para rendimiento).
                        await Task.Run(() => DeleteFolderContents(item.Path, cancellationToken), cancellationToken);
                        cleanedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("No se pudo limpiar la caché en la ruta {Path}: {Msg}", item.Path, ex.Message);
                }

                progress.Report((double)i / itemsToClean.Count * 100);
            }

            progress.Report(100);
            return cleanedCount;
        }

        private long GetFolderSize(string folderPath, CancellationToken cancellationToken)
        {
            long size = 0;
            try
            {
                var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.Exists)
                        {
                            size += info.Length;
                        }
                    }
                    catch { /* Archivo inaccesible */ }
                }
            }
            catch { /* Carpeta inaccesible */ }
            return size;
        }

        private void DeleteFolderContents(string folderPath, CancellationToken cancellationToken)
        {
            try
            {
                var di = new DirectoryInfo(folderPath);
                
                foreach (var file in di.GetFiles("*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        file.Delete();
                    }
                    catch (UnauthorizedAccessException) { /* Archivo en uso por el navegador abierto */ }
                    catch (IOException) { /* Bloqueado */ }
                }

                foreach (var dir in di.GetDirectories("*", SearchOption.AllDirectories).OrderByDescending(d => d.FullName.Length))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        if (dir.Exists && !dir.EnumerateFileSystemInfos().Any())
                        {
                            dir.Delete(false);
                        }
                    }
                    catch { /* Bloqueado o protegido */ }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error parcial al eliminar contenido de la caché: {Msg}", ex.Message);
            }
        }
    }
}
