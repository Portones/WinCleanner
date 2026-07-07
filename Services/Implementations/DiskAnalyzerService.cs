using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class DiskAnalyzerService : IDiskAnalyzerService
    {
        private static readonly string[] MediaExtensions = { ".mp4", ".mkv", ".avi", ".mov", ".mp3", ".wav", ".flac", ".png", ".jpg", ".jpeg", ".gif" };
        private static readonly string[] CompressedExtensions = { ".zip", ".rar", ".7z", ".tar", ".gz", ".iso" };
        private static readonly string[] ExecutableExtensions = { ".exe", ".msi", ".dll", ".sys", ".bat", ".cmd" };
        private static readonly string[] DocumentExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv" };

        public async Task<DiskNode> AnalyzeDirectoryAsync(string path, IProgress<double> progress, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var di = new DirectoryInfo(path);
                if (!di.Exists)
                {
                    throw new DirectoryNotFoundException($"El directorio {path} no existe.");
                }

                progress.Report(0);
                long totalFoldersScanned = 0;
                
                // Escaneo inicial rápido para contar directorios
                try
                {
                    totalFoldersScanned = Directory.GetDirectories(path, "*", SearchOption.AllDirectories).Length;
                }
                catch { }

                long foldersScanned = 0;
                DiskNode root = ScanNode(di, ref foldersScanned, totalFoldersScanned, progress, cancellationToken);
                
                progress.Report(100);
                return root;
            }, cancellationToken);
        }

        private DiskNode ScanNode(DirectoryInfo dirInfo, ref long foldersScanned, long totalFolders, IProgress<double> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var node = new DiskNode
            {
                Name = dirInfo.Name,
                Path = dirInfo.FullName,
                IsFolder = true
            };

            // Saltar enlaces simbólicos / puntos de repetición del sistema para evitar bucles
            if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                return node;
            }

            long totalSize = 0;
            var children = new List<DiskNode>();

            // 1. Escanear archivos en esta carpeta
            try
            {
                var files = dirInfo.GetFiles();
                foreach (var f in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    long fileSize = f.Length;
                    totalSize += fileSize;

                    children.Add(new DiskNode
                    {
                        Name = f.Name,
                        Path = f.FullName,
                        Size = fileSize,
                        IsFolder = false,
                        ColorHex = GetColorForFile(f.Extension),
                        Parent = node
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Verbose("No se pudieron leer archivos del directorio {Path}: {Msg}", dirInfo.FullName, ex.Message);
            }

            // 2. Escanear subdirectorios
            try
            {
                var dirs = dirInfo.GetDirectories();
                foreach (var d in dirs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var childNode = ScanNode(d, ref foldersScanned, totalFolders, progress, cancellationToken);
                    childNode.Parent = node;
                    totalSize += childNode.Size;
                    children.Add(childNode);
                }
            }
            catch (Exception ex)
            {
                Log.Verbose("No se pudieron leer subcarpetas del directorio {Path}: {Msg}", dirInfo.FullName, ex.Message);
            }

            node.Size = totalSize;
            node.Children = children.OrderByDescending(x => x.Size).ToList();

            // Actualizar progresión
            foldersScanned++;
            if (totalFolders > 0 && foldersScanned % 15 == 0)
            {
                double percent = (double)foldersScanned / totalFolders * 100;
                progress.Report(percent > 99 ? 99 : percent);
            }

            return node;
        }

        public void CalculateTreemapLayout(DiskNode rootNode, Rect bounds)
        {
            if (rootNode == null) return;
            CalculateLayoutRecursive(rootNode, bounds, true);
        }

        private void CalculateLayoutRecursive(DiskNode node, Rect bounds, bool horizontal)
        {
            node.Bounds = bounds;

            if (node.Children == null || node.Children.Count == 0) return;

            double totalSize = node.Size;
            if (totalSize == 0) return;

            double currentOffset = 0;
            foreach (var child in node.Children)
            {
                double percentage = (double)child.Size / totalSize;
                
                if (horizontal)
                {
                    double width = bounds.Width * percentage;
                    if (width < 0.5) width = 0.5; // Evitar anchos nulos o negativos

                    var childBounds = new Rect(bounds.Left + currentOffset, bounds.Top, width, bounds.Height);
                    CalculateLayoutRecursive(child, childBounds, !horizontal);
                    currentOffset += width;
                }
                else
                {
                    double height = bounds.Height * percentage;
                    if (height < 0.5) height = 0.5;

                    var childBounds = new Rect(bounds.Left, bounds.Top + currentOffset, bounds.Width, height);
                    CalculateLayoutRecursive(child, childBounds, !horizontal);
                    currentOffset += height;
                }
            }
        }

        private static string GetColorForFile(string extension)
        {
            string ext = extension.ToLowerInvariant();

            if (MediaExtensions.Contains(ext))
                return "#EF4444"; // Rojo (Multimedia)
            
            if (CompressedExtensions.Contains(ext))
                return "#F59E0B"; // Ámbar/Naranja (Comprimidos)
            
            if (ExecutableExtensions.Contains(ext))
                return "#10B981"; // Esmeralda/Verde (Sistemas/Ejecutables)
            
            if (DocumentExtensions.Contains(ext))
                return "#3B82F6"; // Azul (Documentos)

            return "#64748B"; // Pizarra/Gris (Otros)
        }
    }
}
