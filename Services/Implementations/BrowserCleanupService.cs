using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class BrowserCleanupService : IBrowserCleanupService
    {
        // Navegadores: (nombre, subcarpeta relativa al AppData)
        private static readonly (string Name, string BasePath, string[] SubPaths)[] _browsers =
        {
            ("Google Chrome",
             Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data"),
             new[] { "Default", "Profile 1", "Profile 2" }),

            ("Microsoft Edge",
             Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\User Data"),
             new[] { "Default", "Profile 1" }),

            ("Mozilla Firefox",
             Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Mozilla\Firefox\Profiles"),
             new[] { "" }),          // perfiles detectados dinámicamente

            ("Brave Browser",
             Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"BraveSoftware\Brave-Browser\User Data"),
             new[] { "Default", "Profile 1" }),

            ("Opera",
             Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Opera Software\Opera Stable"),
             new[] { "" }),

            ("Vivaldi",
             Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Vivaldi\User Data"),
             new[] { "Default", "Profile 1" }),
        };

        // Carpetas de caché que se pueden limpiar de forma segura dentro de un perfil Chromium
        private static readonly string[] _chromiumCacheDirs =
        {
            "Cache", "Code Cache", "GPUCache", "Service Worker\\CacheStorage",
            "Service Worker\\ScriptCache", "Media Cache", "ShaderCache", "logs"
        };

        public async Task<List<BrowserProfile>> ScanBrowserCacheAsync(
            IProgress<string> progress, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var results = new List<BrowserProfile>();

                foreach (var (name, basePath, subPaths) in _browsers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!Directory.Exists(basePath)) continue;

                    progress.Report($"Escaneando {name}...");

                    // Firefox: iterar directorios de perfiles dinámicamente
                    IEnumerable<string> profiles = name == "Mozilla Firefox"
                        ? Directory.GetDirectories(basePath)
                        : subPaths.Select(sp => Path.Combine(basePath, sp)).Where(Directory.Exists);

                    foreach (var profilePath in profiles)
                    {
                        long totalSize = 0;

                        if (name == "Mozilla Firefox")
                        {
                            // Carpetas de caché de Firefox
                            foreach (var cacheDir in new[] { "cache2", "thumbnails", "startupCache", "logs" })
                            {
                                var dir = Path.Combine(profilePath, cacheDir);
                                if (Directory.Exists(dir))
                                    totalSize += GetDirectorySize(dir);
                            }
                        }
                        else
                        {
                            foreach (var cacheDir in _chromiumCacheDirs)
                            {
                                var dir = Path.Combine(profilePath, cacheDir);
                                if (Directory.Exists(dir))
                                    totalSize += GetDirectorySize(dir);
                            }
                        }

                        if (totalSize > 0)
                        {
                            results.Add(new BrowserProfile
                            {
                                BrowserName   = name,
                                ProfilePath   = profilePath,
                                CacheSizeBytes = totalSize
                            });
                        }
                    }
                }

                return results;
            }, cancellationToken);
        }

        public async Task<long> CleanBrowserCacheAsync(
            IEnumerable<BrowserProfile> profiles, IProgress<double> progress, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                long totalFreed = 0;
                var list = profiles.ToList();

                for (int i = 0; i < list.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var profile = list[i];

                    try
                    {
                        string[] cacheDirs = profile.BrowserName == "Mozilla Firefox"
                            ? new[] { "cache2", "thumbnails", "startupCache", "logs" }
                            : _chromiumCacheDirs;

                        foreach (var cacheDir in cacheDirs)
                        {
                            var dir = Path.Combine(profile.ProfilePath, cacheDir);
                            if (!Directory.Exists(dir)) continue;
                            totalFreed += DeleteDirectoryContents(dir);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error al limpiar caché del perfil {Profile}", profile.ProfilePath);
                    }

                    progress.Report((double)(i + 1) / list.Count * 100);
                }

                return totalFreed;
            }, cancellationToken);
        }

        // ────── helpers ──────
        private static long GetDirectorySize(string path)
        {
            try
            {
                return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                                .Sum(f => { try { return new FileInfo(f).Length; } catch { return 0L; } });
            }
            catch { return 0L; }
        }

        private static long DeleteDirectoryContents(string path)
        {
            long freed = 0;
            try
            {
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        freed += fi.Length;
                        fi.Delete();
                    }
                    catch { }
                }
                // Eliminar subdirectorios vacíos
                foreach (var dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories)
                                             .OrderByDescending(d => d.Length))
                {
                    try { Directory.Delete(dir, false); } catch { }
                }
            }
            catch { }
            return freed;
        }
    }
}
