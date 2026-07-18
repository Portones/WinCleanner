using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class WinCleanerUpdateService : IWinCleanerUpdateService
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/Portones/WinCleanner/releases/latest";
        private static readonly HttpClient _httpClient = new();

        static WinCleanerUpdateService()
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("WinCleanerApp", "1.0"));
            _httpClient.Timeout = TimeSpan.FromSeconds(20);
        }

        public async Task<WinCleanerUpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
        {
            var info = new WinCleanerUpdateInfo
            {
                CurrentVersion = GetCurrentAppVersion()
            };

            try
            {
                Log.Information("Buscando actualizaciones de WinCleaner en GitHub Releases...");
                using var response = await _httpClient.GetAsync(GitHubApiUrl, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    Log.Warning("Respuesta no satisfactoria al consultar GitHub API: {StatusCode}", response.StatusCode);
                    return info;
                }

                string jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                // Extraer tag_name (ej: "v1.8.0" -> "1.8.0")
                string tagName = root.GetProperty("tag_name").GetString() ?? "";
                string cleanTag = tagName.TrimStart('v', 'V');
                info.LatestVersion = cleanTag;

                // Extraer notas de la versión (body)
                if (root.TryGetProperty("body", out var bodyProp))
                {
                    info.ReleaseNotes = bodyProp.GetString() ?? string.Empty;
                }

                // Extraer fecha de publicación
                if (root.TryGetProperty("published_at", out var publishedProp) && publishedProp.TryGetDateTime(out var pubDate))
                {
                    info.PublishedAt = pubDate;
                }

                // Buscar asset ejecutable de instalación (.exe)
                if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assetsProp.EnumerateArray())
                    {
                        string name = asset.GetProperty("name").GetString() ?? "";
                        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            info.DownloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                            if (asset.TryGetProperty("size", out var sizeProp))
                            {
                                info.FileSizeBytes = sizeProp.GetInt64();
                            }
                            break;
                        }
                    }
                }

                // Comparar versiones
                if (Version.TryParse(info.CurrentVersion, out var currentVer) &&
                    Version.TryParse(info.LatestVersion, out var latestVer))
                {
                    info.IsUpdateAvailable = latestVer > currentVer;
                }
                else
                {
                    // Fallback de comparación string
                    info.IsUpdateAvailable = !string.Equals(info.CurrentVersion, info.LatestVersion, StringComparison.OrdinalIgnoreCase) &&
                                             !string.IsNullOrEmpty(info.LatestVersion);
                }

                Log.Information("Comprobación de actualizaciones finalizada. Versión actual: {Current}, Última: {Latest}, Disponible: {Available}",
                    info.CurrentVersion, info.LatestVersion, info.IsUpdateAvailable);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al comprobar actualizaciones de WinCleaner desde GitHub.");
            }

            return info;
        }

        public async Task<bool> DownloadAndInstallUpdateAsync(string downloadUrl, IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(downloadUrl))
            {
                Log.Warning("No se proporcionó una URL de descarga válida para la actualización.");
                return false;
            }

            string tempPath = Path.Combine(Path.GetTempPath(), "WinCleanerSetup_Update.exe");

            try
            {
                Log.Information("Descargando actualización de WinCleaner desde {Url}...", downloadUrl);

                using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;

                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalRead = 0;
                int read;

                while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read, cancellationToken);
                    totalRead += read;

                    if (totalBytes.HasValue && totalBytes.Value > 0)
                    {
                        double percentage = (double)totalRead / totalBytes.Value * 100;
                        progress?.Report(percentage);
                    }
                }

                fileStream.Close();
                Log.Information("Descarga de la actualización completada en: {Path}", tempPath);

                // Iniciar instalador de Inno Setup
                Log.Information("Ejecutando instalador para actualizar WinCleaner...");
                var startInfo = new ProcessStartInfo
                {
                    FileName = tempPath,
                    Arguments = "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
                    UseShellExecute = true
                };

                Process.Start(startInfo);

                // Cerrar la aplicación actual para permitir la sobreescritura
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.Application.Current.Shutdown();
                });

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error durante la descarga o ejecución del instalador de actualización.");
                return false;
            }
        }

        private static string GetCurrentAppVersion()
        {
            try
            {
                var uri = new Uri("/WinCleaner;component/Views/MainWindow.xaml", UriKind.Relative);
                var streamInfo = System.Windows.Application.GetResourceStream(uri);
                if (streamInfo != null)
                {
                    using var reader = new StreamReader(streamInfo.Stream);
                    string content = reader.ReadToEnd();
                    var match = System.Text.RegularExpressions.Regex.Match(content, @"Versión\s+(\d+\.\d+\.\d+)");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "No se pudo leer la versión de MainWindow.xaml en runtime.");
            }

            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    return $"{version.Major}.{version.Minor}.{version.Build}";
                }
            }
            catch
            {
                // Ignorar
            }
            return "1.9.0";
        }
    }
}
