using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class AppIconProvider : IAppIconProvider
    {
        private readonly ConcurrentDictionary<string, ImageSource?> _iconCache = new(StringComparer.OrdinalIgnoreCase);

        public ImageSource? GetAppIcon(string iconPath, string installLocation)
        {
            string cacheKey = $"{iconPath}|{installLocation}";
            if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
            {
                return cachedIcon;
            }

            var icon = ExtractIcon(iconPath, installLocation);
            _iconCache[cacheKey] = icon;
            return icon;
        }

        private static ImageSource? ExtractIcon(string iconPath, string installLocation)
        {
            try
            {
                string targetPath = string.Empty;

                if (!string.IsNullOrEmpty(iconPath))
                {
                    // Si DisplayIcon contiene índice (ej. "C:\Program.exe,0")
                    string cleanPath = iconPath.Split(',')[0].Trim('"');
                    if (File.Exists(cleanPath))
                    {
                        targetPath = cleanPath;
                    }
                }

                if (string.IsNullOrEmpty(targetPath) && !string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                {
                    // Intentar buscar el primer .exe en la carpeta de instalación
                    var exeFiles = Directory.GetFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly);
                    if (exeFiles.Length > 0)
                    {
                        targetPath = exeFiles[0];
                    }
                }

                if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
                {
                    return null;
                }

                using (var sysIcon = Icon.ExtractAssociatedIcon(targetPath))
                {
                    if (sysIcon == null) return null;

                    var bs = Imaging.CreateBitmapSourceFromHIcon(
                        sysIcon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    
                    bs.Freeze();
                    return bs;
                }
            }
            catch (Exception ex)
            {
                Log.Verbose("No se pudo extraer icono para {Path}: {Msg}", iconPath, ex.Message);
                return null;
            }
        }
    }
}
