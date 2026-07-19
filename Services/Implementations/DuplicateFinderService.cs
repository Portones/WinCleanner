using System;
using System.Collections.Concurrent;
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
                    await Task.Run(() =>
                    {
                        foreach (var fi in SafeDirectoryEnumerator.EnumerateFilesSafe(path))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            try
                            {
                                if (fi.Exists && fi.Length > 1024)
                                {
                                    allFiles.Add(fi);
                                }
                            }
                            catch { }
                        }
                    }, cancellationToken);
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

            Log.Information("Encontrados {Count} grupos de tamaño con posibles duplicados. Aplicando filtro de Hash Parcial...", sizeGroups.Count);

            // 3. Segunda Optimización: Filtrar por Hash Parcial (primeros 4KB)
            var candidateGroups = new List<List<FileInfo>>();
            int processedSizeGroups = 0;

            foreach (var sizeGroup in sizeGroups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var partialHashGroups = sizeGroup
                    .Select(f => new { File = f, PartialHash = CalculatePartialHash(f.FullName) })
                    .Where(x => !string.IsNullOrEmpty(x.PartialHash))
                    .GroupBy(x => x.PartialHash)
                    .Where(g => g.Count() > 1);

                foreach (var phGroup in partialHashGroups)
                {
                    candidateGroups.Add(phGroup.Select(x => x.File).ToList());
                }

                processedSizeGroups++;
                progress.Report(20.0 + ((double)processedSizeGroups / sizeGroups.Count * 20.0)); // 20% a 40% asignado a hash parcial
            }

            if (candidateGroups.Count == 0)
            {
                progress.Report(100);
                return groups;
            }

            Log.Information("Encontrados {Count} grupos candidatos tras Hash Parcial. Calculando SHA-256 completo en paralelo...", candidateGroups.Count);

            // 4. Tercera Optimización: Calcular hash SHA-256 completo en paralelo usando Parallel.ForEachAsync
            var hashDictionary = new ConcurrentDictionary<string, ConcurrentBag<FileInfo>>();
            int totalCandidates = candidateGroups.Sum(g => g.Count);
            int processedCandidates = 0;

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            };

            foreach (var candidateGroup in candidateGroups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Parallel.ForEachAsync(candidateGroup, parallelOptions, async (file, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var fullHash = await Task.Run(() => CalculateSHA256(file.FullName), ct);
                        if (!string.IsNullOrEmpty(fullHash))
                        {
                            var bag = hashDictionary.GetOrAdd(fullHash, _ => new ConcurrentBag<FileInfo>());
                            bag.Add(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("No se pudo procesar hash completo para {Path}: {Msg}", file.FullName, ex.Message);
                    }
                    finally
                    {
                        int currentProcessed = Interlocked.Increment(ref processedCandidates);
                        double calcProgress = 40.0 + ((double)currentProcessed / totalCandidates * 60.0);
                        progress.Report(calcProgress);
                    }
                });
            }

            // 5. Agrupar resultados y descartar aquellos que no tengan duplicados reales (grupos de 1 elemento)
            foreach (var kvp in hashDictionary)
            {
                var fileList = kvp.Value.ToList();
                if (fileList.Count > 1)
                {
                    var fileInfoSample = fileList[0];
                    var group = new DuplicateGroup
                    {
                        Hash = kvp.Key,
                        Size = fileInfoSample.Length,
                        Files = fileList.Select(f => new DuplicateFile
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

        public static string CalculatePartialHash(string filePath, int bytesToRead = 4096)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] buffer = new byte[bytesToRead];
                int bytesRead = stream.Read(buffer, 0, bytesToRead);
                if (bytesRead <= 0) return string.Empty;

                using var sha256 = SHA256.Create();
                byte[] hashBytes = sha256.ComputeHash(buffer, 0, bytesRead);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
            catch
            {
                return string.Empty;
            }
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
