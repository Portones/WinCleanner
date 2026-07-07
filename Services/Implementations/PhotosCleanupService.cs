using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class PhotosCleanupService : IPhotosCleanupService
    {
        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };

        public async Task<List<PhotoItem>> GetObsoleteScreenshotsAsync(int ageInDays, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var list = new List<PhotoItem>();
                var foldersToScan = new List<string>
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
                };

                // Añadir subcarpeta de Capturas de Pantalla explícita si existe
                string screenshotsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
                if (Directory.Exists(screenshotsPath) && !foldersToScan.Contains(screenshotsPath))
                {
                    foldersToScan.Add(screenshotsPath);
                }

                foreach (var folder in foldersToScan)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!Directory.Exists(folder)) continue;

                    try
                    {
                        var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                            .Where(file => ImageExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()));

                        foreach (var file in files)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var fi = new FileInfo(file);

                            // Validar si el nombre sugiere una captura de pantalla
                            bool isScreenshotName = fi.Name.Contains("Screenshot", StringComparison.OrdinalIgnoreCase) ||
                                                    fi.Name.Contains("Captura", StringComparison.OrdinalIgnoreCase);

                            if (isScreenshotName)
                            {
                                // Validar antigüedad
                                double fileAge = (DateTime.Now - fi.CreationTime).TotalDays;
                                if (fileAge > ageInDays)
                                {
                                    list.Add(new PhotoItem
                                    {
                                        Name = fi.Name,
                                        Path = fi.FullName,
                                        Size = fi.Length,
                                        DateCreated = fi.CreationTime,
                                        Thumbnail = CreateThumbnail(fi.FullName),
                                        IsSelected = true // Seleccionadas por defecto
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Error al escanear carpeta {Folder} para capturas obsoletas: {Msg}", folder, ex.Message);
                    }
                }

                return list.OrderByDescending(x => x.DateCreated).ToList();
            }, cancellationToken);
        }

        public async Task<List<DuplicatePhotoGroup>> GetDuplicatePhotosAsync(string scanPath, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var result = new List<DuplicatePhotoGroup>();
                if (!Directory.Exists(scanPath)) return result;

                try
                {
                    // 1. Encontrar todos los archivos de imagen
                    var files = Directory.EnumerateFiles(scanPath, "*.*", SearchOption.AllDirectories)
                        .Where(file => ImageExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                        .Select(f => new FileInfo(f))
                        .ToList();

                    // 2. Agrupar por tamaño de archivo para evitar calcular hashes de todo
                    var sizeGroups = files.GroupBy(f => f.Length)
                                         .Where(g => g.Count() > 1)
                                         .ToList();

                    // 3. Para archivos con el mismo tamaño, calcular hash y agrupar
                    var hashDictionary = new Dictionary<string, List<FileInfo>>();

                    foreach (var group in sizeGroups)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        foreach (var file in group)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            string hash = GetFileHash(file.FullName);
                            if (string.IsNullOrEmpty(hash)) continue;

                            if (!hashDictionary.ContainsKey(hash))
                            {
                                hashDictionary[hash] = new List<FileInfo>();
                            }
                            hashDictionary[hash].Add(file);
                        }
                    }

                    // 4. Filtrar solo los grupos que tengan más de una coincidencia de hash
                    var duplicateGroups = hashDictionary.Where(kvp => kvp.Value.Count > 1).ToList();

                    foreach (var kvp in duplicateGroups)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var group = new DuplicatePhotoGroup { Hash = kvp.Key };

                        foreach (var fi in kvp.Value)
                        {
                            group.Photos.Add(new PhotoItem
                            {
                                Name = fi.Name,
                                Path = fi.FullName,
                                Size = fi.Length,
                                DateCreated = fi.CreationTime,
                                Thumbnail = CreateThumbnail(fi.FullName),
                                IsSelected = false // No seleccionado por defecto para evitar borrar ambos por error
                            });
                        }

                        result.Add(group);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al buscar fotos duplicadas en {Path}", scanPath);
                }

                return result;
            }, cancellationToken);
        }

        public async Task<bool> DeletePhotoAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        Log.Information("Archivo eliminado con éxito: {Path}", filePath);
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al eliminar archivo {Path}", filePath);
                    return false;
                }
            });
        }

        private static ImageSource? CreateThumbnail(string path)
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path);
                image.DecodePixelWidth = 120; // Resolución miniatura baja y eficiente
                image.CacheOption = BitmapCacheOption.OnLoad; // Leer completo y cerrar flujo de archivo
                image.EndInit();
                image.Freeze(); // Desbloquear archivo y optimizar rendimiento de WPF
                return image;
            }
            catch
            {
                return null;
            }
        }

        private static string GetFileHash(string filePath)
        {
            try
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                byte[] hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
            catch (Exception ex)
            {
                Log.Verbose("No se pudo calcular el hash de {Path}: {Msg}", filePath, ex.Message);
                return string.Empty;
            }
        }
    }
}
