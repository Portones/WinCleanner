using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class AppUninstallerService : IAppUninstallerService
    {
        public async Task<List<InstalledApp>> GetInstalledAppsAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var apps = new List<InstalledApp>();

                // 1. Cargar aplicaciones Win32 desde el Registro (64 bits, 32 bits y Usuario)
                cancellationToken.ThrowIfCancellationRequested();
                LoadWin32Apps(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", false, apps);
                LoadWin32Apps(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", false, apps);
                LoadWin32Apps(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true, apps);

                // 2. Cargar aplicaciones UWP/Store usando APIs nativas de Windows Runtime
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    LoadUwpApps(apps);
                }
                catch (Exception ex)
                {
                    Log.Warning("No se pudieron cargar las aplicaciones UWP/Store (puede no ser compatible con esta edición de Windows): {Msg}", ex.Message);
                }

                // Filtrar duplicados por nombre y ruta de desinstalación y ordenar por Nombre de forma alfabética
                return apps
                    .GroupBy(x => new { Name = x.DisplayName.ToLowerInvariant(), x.UninstallString })
                    .Select(g => g.First())
                    .OrderBy(x => x.DisplayName)
                    .ToList();
            }, cancellationToken);
        }

        private void LoadWin32Apps(string registryPath, bool useCurrentUser, List<InstalledApp> apps)
        {
            var rootKey = useCurrentUser ? Registry.CurrentUser : Registry.LocalMachine;
            try
            {
                using (var key = rootKey.OpenSubKey(registryPath, false))
                {
                    if (key == null) return;

                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using (var subKey = key.OpenSubKey(subKeyName, false))
                        {
                            if (subKey == null) continue;

                            string displayName = subKey.GetValue("DisplayName")?.ToString() ?? string.Empty;
                            string uninstallString = subKey.GetValue("UninstallString")?.ToString() ?? string.Empty;

                            // Ignorar actualizaciones, parches o programas sin desinstalador
                            if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(uninstallString))
                                continue;

                            // Evitar registrar componentes del sistema o actualizaciones de seguridad de Windows
                            bool isSystemComponent = subKey.GetValue("SystemComponent")?.ToString() == "1";
                            bool isUpdate = subKey.GetValue("ParentKeyName") != null || displayName.Contains("Security Update") || displayName.Contains("KB9");
                            if (isSystemComponent || isUpdate)
                                continue;

                            string publisher = subKey.GetValue("Publisher")?.ToString() ?? string.Empty;
                            string version = subKey.GetValue("DisplayVersion")?.ToString() ?? string.Empty;
                            string installLocation = subKey.GetValue("InstallLocation")?.ToString() ?? string.Empty;
                            string iconPath = subKey.GetValue("DisplayIcon")?.ToString() ?? string.Empty;

                            // Tamaño estimado
                            long size = 0;
                            var sizeVal = subKey.GetValue("EstimatedSize");
                            if (sizeVal != null && long.TryParse(sizeVal.ToString(), out var sizeKb))
                            {
                                size = sizeKb * 1024; // Convertir de KB a Bytes
                            }

                            // Fecha de instalación
                            DateTime? installDate = null;
                            var dateVal = subKey.GetValue("InstallDate")?.ToString();
                            if (!string.IsNullOrEmpty(dateVal) && dateVal.Length == 8)
                            {
                                if (DateTime.TryParseExact(dateVal, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date))
                                {
                                    installDate = date;
                                }
                            }

                            apps.Add(new InstalledApp
                            {
                                Name = subKeyName,
                                DisplayName = displayName,
                                Publisher = publisher,
                                DisplayVersion = version,
                                EstimatedSize = size,
                                UninstallString = uninstallString,
                                InstallLocation = installLocation,
                                IconPath = iconPath,
                                IsUwp = false
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error al leer aplicaciones de registro en {Path}: {Msg}", registryPath, ex.Message);
            }
        }

        private void LoadUwpApps(List<InstalledApp> apps)
        {
            var pm = new Windows.Management.Deployment.PackageManager();
            // Buscar paquetes del usuario actual
            var packages = pm.FindPackagesForUser(string.Empty);

            foreach (var pkg in packages)
            {
                try
                {
                    // Omitir frameworks, recursos o paquetes de desarrollo (dependencias silenciosas)
                    if (pkg.IsFramework || pkg.IsResourcePackage || pkg.IsDevelopmentMode)
                        continue;

                    string displayName = pkg.Id.Name;
                    try
                    {
                        displayName = pkg.DisplayName;
                    }
                    catch { }

                    // Si no tiene nombre amigable, omitir o usar el identificador básico
                    if (string.IsNullOrEmpty(displayName))
                        continue;

                    // Omitir componentes nativos esenciales de Windows
                    if (pkg.SignatureKind == Windows.ApplicationModel.PackageSignatureKind.System || 
                        pkg.Id.PublisherId == "cw5n1h2txyewy") // Clave de firmas nativas de Microsoft
                        continue;

                    string publisher = pkg.PublisherDisplayName;
                    string version = $"{pkg.Id.Version.Major}.{pkg.Id.Version.Minor}.{pkg.Id.Version.Build}.{pkg.Id.Version.Revision}";
                    string installLocation = string.Empty;
                    long size = 0;

                    try
                    {
                        installLocation = pkg.InstalledLocation?.Path ?? string.Empty;
                        if (!string.IsNullOrEmpty(installLocation))
                        {
                            size = GetFolderSize(installLocation);
                        }
                    }
                    catch { }

                    DateTime? installDate = null;
                    try
                    {
                        installDate = pkg.InstalledDate.DateTime;
                    }
                    catch { }

                    apps.Add(new InstalledApp
                    {
                        Name = pkg.Id.Name,
                        DisplayName = displayName,
                        Publisher = publisher,
                        DisplayVersion = version,
                        EstimatedSize = size,
                        InstallLocation = installLocation,
                        PackageFullName = pkg.Id.FullName,
                        IsUwp = true
                    });
                }
                catch { }
            }
        }

        public async Task<bool> UninstallAppAsync(InstalledApp app, CancellationToken cancellationToken)
        {
            if (app.IsUwp)
            {
                return await Task.Run(async () =>
                {
                    try
                    {
                        var pm = new Windows.Management.Deployment.PackageManager();
                        var operation = pm.RemovePackageAsync(app.PackageFullName);
                        
                        var tcs = new TaskCompletionSource<bool>();
                        operation.Completed = (op, status) =>
                        {
                            if (status == Windows.Foundation.AsyncStatus.Completed)
                            {
                                Log.Information("Aplicación UWP {Name} desinstalada correctamente.", app.DisplayName);
                                tcs.SetResult(true);
                            }
                            else
                            {
                                var err = op.ErrorCode;
                                Log.Error("Error al desinstalar UWP {Name}: {Code}", app.DisplayName, err);
                                tcs.SetException(new Exception($"Código de error UWP: {err.Message}"));
                            }
                        };

                        return await tcs.Task;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error al desinstalar aplicación UWP/Store {Name}.", app.DisplayName);
                        throw;
                    }
                });
            }
            else
            {
                return await Task.Run(async () =>
                {
                    try
                    {
                        string uninstallStr = app.UninstallString.Trim();
                        string exe;
                        string args = string.Empty;

                        if (uninstallStr.StartsWith("\""))
                        {
                            int nextQuote = uninstallStr.IndexOf("\"", 1);
                            if (nextQuote > 0)
                            {
                                exe = uninstallStr.Substring(1, nextQuote - 1);
                                args = uninstallStr.Substring(nextQuote + 1).Trim();
                            }
                            else
                            {
                                exe = uninstallStr.Replace("\"", "");
                            }
                        }
                        else
                        {
                            int firstSpace = uninstallStr.IndexOf(" ");
                            if (firstSpace > 0)
                            {
                                exe = uninstallStr.Substring(0, firstSpace);
                                args = uninstallStr.Substring(firstSpace + 1).Trim();
                            }
                            else
                            {
                                exe = uninstallStr;
                            }
                        }

                        var startInfo = new ProcessStartInfo
                        {
                            FileName = exe,
                            Arguments = args,
                            UseShellExecute = true // Necesario para elevación UAC del desinstalador
                        };

                        using (var process = Process.Start(startInfo))
                        {
                            if (process != null)
                            {
                                await process.WaitForExitAsync(cancellationToken);
                                Log.Information("Desinstalador Win32 de {Name} finalizó con código {Code}.", app.DisplayName, process.ExitCode);
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error al lanzar el desinstalador Win32 para {Name}.", app.DisplayName);
                        throw;
                    }
                    return false;
                });
            }
        }

        public async Task<List<ResidualItem>> ScanResidualsAsync(InstalledApp app, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var residuals = new List<ResidualItem>();
                string appName = app.Name;
                string displayName = app.DisplayName;
                string publisher = app.Publisher;

                // Definir palabras clave para buscar directorios huérfanos
                var keywords = new List<string>();
                if (!string.IsNullOrEmpty(appName) && appName.Length > 3) keywords.Add(appName);
                if (!string.IsNullOrEmpty(displayName) && displayName.Length > 3) keywords.Add(displayName);

                // Evitar palabras clave genéricas de sistema
                var cleanKeywords = keywords
                    .Select(k => k.Replace(" ", "").ToLowerInvariant())
                    .Where(k => k != "setup" && k != "install" && k != "uninstall" && k != "windows")
                    .Distinct()
                    .ToList();

                if (cleanKeywords.Count == 0) return residuals;

                // 1. Directorios de Archivos Comunes
                var searchPaths = new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), // Roaming
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), // Local
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                };

                foreach (var basePath in searchPaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!Directory.Exists(basePath)) continue;

                    try
                    {
                        // Escanear carpetas de nivel superior
                        var directories = Directory.GetDirectories(basePath);
                        foreach (var dir in directories)
                        {
                            string dirName = Path.GetFileName(dir).ToLowerInvariant();
                            
                            // Comprobar si coincide con alguna palabra clave del programa
                            if (cleanKeywords.Any(k => dirName.Contains(k)))
                            {
                                // Confirmar que la carpeta de instalación no esté activa 
                                // (si el desinstalador la vació, pero dejó la carpeta madre)
                                long size = GetFolderSize(dir);
                                residuals.Add(new ResidualItem
                                {
                                    Path = dir,
                                    Type = "Carpeta",
                                    Size = size
                                });
                            }
                            // Comprobar estructura de tipo Publisher\AppName
                            else if (!string.IsNullOrEmpty(publisher) && publisher.Length > 3 && dirName.Contains(publisher.ToLowerInvariant()))
                            {
                                try
                                {
                                    var subDirs = Directory.GetDirectories(dir);
                                    foreach (var subDir in subDirs)
                                    {
                                        string subName = Path.GetFileName(subDir).ToLowerInvariant();
                                        if (cleanKeywords.Any(k => subName.Contains(k)))
                                        {
                                            long size = GetFolderSize(subDir);
                                            residuals.Add(new ResidualItem
                                            {
                                                Path = subDir,
                                                Type = "Carpeta",
                                                Size = size
                                            });
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }

                // 2. Claves de Registro Huérfanas (HKCU y HKLM)
                cancellationToken.ThrowIfCancellationRequested();
                ScanRegistryResiduals(Registry.CurrentUser, @"Software", cleanKeywords, publisher, residuals);
                ScanRegistryResiduals(Registry.LocalMachine, @"Software", cleanKeywords, publisher, residuals);

                return residuals;
            }, cancellationToken);
        }

        private void ScanRegistryResiduals(RegistryKey rootKey, string baseSubKey, List<string> keywords, string publisher, List<ResidualItem> residuals)
        {
            try
            {
                using (var key = rootKey.OpenSubKey(baseSubKey, false))
                {
                    if (key == null) return;

                    foreach (var subName in key.GetSubKeyNames())
                    {
                        string cleanSubName = subName.ToLowerInvariant();

                        // Buscar coincidencia directa de clave
                        if (keywords.Any(k => cleanSubName.Contains(k)))
                        {
                            residuals.Add(new ResidualItem
                            {
                                Path = $@"{rootKey.Name}\{baseSubKey}\{subName}",
                                Type = "Registro",
                                Size = 0
                            });
                        }
                        // Buscar coincidencia de publicador (ej. Software\Publisher\AppName)
                        else if (!string.IsNullOrEmpty(publisher) && publisher.Length > 3 && cleanSubName.Contains(publisher.ToLowerInvariant()))
                        {
                            try
                            {
                                using (var pubKey = key.OpenSubKey(subName, false))
                                {
                                    if (pubKey != null)
                                    {
                                        foreach (var appKeyName in pubKey.GetSubKeyNames())
                                        {
                                            if (keywords.Any(k => appKeyName.ToLowerInvariant().Contains(k)))
                                            {
                                                residuals.Add(new ResidualItem
                                                {
                                                    Path = $@"{rootKey.Name}\{baseSubKey}\{subName}\{appKeyName}",
                                                    Type = "Registro",
                                                    Size = 0
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }

        public async Task<bool> CleanResidualsAsync(List<ResidualItem> residuals, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                bool allSuccess = true;
                foreach (var item in residuals)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!item.IsSelected) continue;

                    try
                    {
                        if (item.Type == "Carpeta")
                        {
                            if (Directory.Exists(item.Path))
                            {
                                Directory.Delete(item.Path, true);
                                Log.Information("Carpeta residual eliminada: {Path}", item.Path);
                            }
                        }
                        else if (item.Type == "Archivo")
                        {
                            if (File.Exists(item.Path))
                            {
                                File.Delete(item.Path);
                                Log.Information("Archivo residual eliminado: {Path}", item.Path);
                            }
                        }
                        else if (item.Type == "Registro")
                        {
                            // Parsear ruta de registro (ej. HKEY_CURRENT_USER\Software\AppName)
                            string[] parts = item.Path.Split('\\');
                            if (parts.Length > 2)
                            {
                                var rootKey = parts[0] == "HKEY_CURRENT_USER" ? Registry.CurrentUser : Registry.LocalMachine;
                                string subKeyPath = string.Join('\\', parts.Skip(1));
                                
                                rootKey.DeleteSubKeyTree(subKeyPath, false);
                                Log.Information("Clave de registro residual eliminada: {Path}", item.Path);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        allSuccess = false;
                        Log.Warning("No se pudo eliminar el residuo {Path}: {Msg}", item.Path, ex.Message);
                    }
                }
                return allSuccess;
            }, cancellationToken);
        }

        public async Task<bool> ForceRemoveAppEntryAsync(InstalledApp app)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string[] registryPaths = new[]
                    {
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                        @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                    };

                    foreach (var path in registryPaths)
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(path, true))
                        {
                            if (key != null && key.GetSubKeyNames().Contains(app.Name))
                            {
                                key.DeleteSubKeyTree(app.Name, false);
                                Log.Information("Clave de desinstalación eliminada de HKLM\\{Path}\\{Name}", path, app.Name);
                                return true;
                            }
                        }

                        using (var key = Registry.CurrentUser.OpenSubKey(path, true))
                        {
                            if (key != null && key.GetSubKeyNames().Contains(app.Name))
                            {
                                key.DeleteSubKeyTree(app.Name, false);
                                Log.Information("Clave de desinstalación eliminada de HKCU\\{Path}\\{Name}", path, app.Name);
                                return true;
                            }
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al forzar la eliminación de la entrada de registro para {Name}", app.DisplayName);
                    return false;
                }
            });
        }

        private static long GetFolderSize(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return 0;
            try
            {
                return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                    .Select(f => { try { return new FileInfo(f).Length; } catch { return 0; } })
                    .Sum();
            }
            catch
            {
                return 0;
            }
        }
    }
}
