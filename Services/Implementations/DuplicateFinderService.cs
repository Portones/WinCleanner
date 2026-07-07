using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Helpers;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class DuplicateFinderService : IDuplicateFinderService
    {
        public async Task<List<DuplicateGroup>> FindDuplicatesAsync(List<string> paths, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var groups = new List<DuplicateGroup>();
            var allFiles = new List<FileInfo>();
            progress.Report(0);

            if (paths == null || paths.Count == 0) return groups;

            // 1. Obtener lista de todos los archivos en segundo plano
            Log.Information("Iniciando escaneo de archivos para buscar duplicados en {PathsCount} rutas.", paths.Count);
            
            int folderCount = 0;
            foreach (var path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Directory.Exists(path))
                {
                    folderCount++;
                    continue;
                }

                try
                {
                    var files = await Task.Run(() => Directory.GetFiles(path, "*.*", SearchOption.AllDirectories), cancellationToken);
                    foreach (var file in files)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            var fi = new FileInfo(file);
                            // Omitir archivos menores de 1 KB para evitar ruido de logs o vacíos repetitivos
                            if (fi.Exists && fi.Length > 1024)
                            {
                                allFiles.Add(fi);
                            }
                        }
                        catch { /* Ignorar bloqueados */ }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al escanear directorio {Path} en búsqueda de duplicados.", path);
                }
                
                folderCount++;
                progress.Report((double)folderCount / paths.Count * 20); // 20% max asignado al listado inicial
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 2. Primera Optimización: Agrupar por tamaño (en bytes)
            var sizeGroups = allFiles.GroupBy(f => f.Length).Where(g => g.Count() > 1).ToList();
            if (sizeGroups.Count == 0)
            {
                progress.Report(100);
                return groups;
            }

            Log.Information("Encontrados {Count} grupos de tamaño con posibles duplicados. Calculando SHA-256...", sizeGroups.Count);

            // 3. Calcular hash SHA-256 únicamente para archivos que comparten el mismo tamaño
            var hashDictionary = new Dictionary<string, List<FileInfo>>();
            int processedGroups = 0;

            foreach (var sizeGroup in sizeGroups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var file in sizeGroup)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var hash = await Task.Run(() => CalculateSHA256(file.FullName), cancellationToken);
                        if (string.IsNullOrEmpty(hash)) continue;

                        if (!hashDictionary.TryGetValue(hash, out var fileList))
                        {
                            fileList = new List<FileInfo>();
                            hashDictionary[hash] = fileList;
                        }
                        fileList.Add(file);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("No se pudo procesar hash para {Path}: {Msg}", file.FullName, ex.Message);
                    }
                }

                processedGroups++;
                double calcProgress = 20.0 + ((double)processedGroups / sizeGroups.Count * 80.0);
                progress.Report(calcProgress);
            }

            // 4. Agrupar resultados y descartar aquellos que no tengan duplicados reales (grupos de 1 elemento)
            foreach (var kvp in hashDictionary)
            {
                if (kvp.Value.Count > 1)
                {
                    var fileInfoSample = kvp.Value[0];
                    var group = new DuplicateGroup
                    {
                        Hash = kvp.Key,
                        Size = fileInfoSample.Length,
                        Files = kvp.Value.Select(f => new DuplicateFile
                        {
                            Path = f.FullName,
                            Name = f.Name,
                            Size = f.Length,
                            LastModified = f.LastWriteTime,
                            IsSelected = false // Ninguno seleccionado por defecto
                        }).ToList()
                    };
                    groups.Add(group);
                }
            }

            Log.Information("Escaneo completado. Encontrados {GroupsCount} grupos de archivos duplicados.", groups.Count);
            progress.Report(100);
            return groups.OrderByDescending(g => g.Size).ToList();
        }

        public async Task<int> CleanDuplicatesAsync(List<DuplicateFile> filesToClean, bool permanent, IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (filesToClean == null || filesToClean.Count == 0) return 0;

            int cleanedCount = 0;
            for (int i = 0; i < filesToClean.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var file = filesToClean[i];

                try
                {
                    if (File.Exists(file.Path))
                    {
                        if (permanent)
                        {
                            File.Delete(file.Path);
                            cleanedCount++;
                        }
                        else
                        {
                            bool success = ShellFileOperations.SendToRecycleBin(file.Path);
                            if (success) cleanedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al eliminar duplicado {Path}", file.Path);
                }

                progress.Report((double)i / filesToClean.Count * 100);
            }

            progress.Report(100);
            return cleanedCount;
        }

        private static string CalculateSHA256(string filePath)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        var hashBytes = sha256.ComputeHash(stream);
                        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
