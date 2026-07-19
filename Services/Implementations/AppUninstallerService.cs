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
        private readonly IRegistryAppScanner _appScanner;
        private readonly IAppIconProvider _iconProvider;

        public AppUninstallerService(IRegistryAppScanner appScanner, IAppIconProvider iconProvider)
        {
            _appScanner = appScanner ?? throw new ArgumentNullException(nameof(appScanner));
            _iconProvider = iconProvider ?? throw new ArgumentNullException(nameof(iconProvider));
        }

        public async Task<List<InstalledApp>> GetInstalledAppsAsync(CancellationToken cancellationToken)
        {
            return await _appScanner.ScanInstalledAppsAsync(cancellationToken);
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

                // 3. Residuos en el Inicio de Windows (Registro y Carpetas Startup)
                cancellationToken.ThrowIfCancellationRequested();
                ScanStartupResiduals(cleanKeywords, residuals);

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

                        if (keywords.Any(k => cleanSubName.Contains(k)))
                        {
                            residuals.Add(new ResidualItem
                            {
                                Path = $@"{rootKey.Name}\{baseSubKey}\{subName}",
                                Type = "Registro",
                                Size = 0
                            });
                        }
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

        private void ScanStartupResiduals(List<string> keywords, List<ResidualItem> residuals)
        {
            var startupRegKeys = new (RegistryKey root, string path)[]
            {
                (Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"),
                (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"),
                (Registry.LocalMachine, @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Run"),
                (Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce"),
                (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce")
            };

            foreach (var (root, path) in startupRegKeys)
            {
                try
                {
                    using (var key = root.OpenSubKey(path, false))
                    {
                        if (key == null) continue;
                        foreach (var valueName in key.GetValueNames())
                        {
                            string valData = key.GetValue(valueName)?.ToString() ?? string.Empty;
                            string nameLower = valueName.ToLowerInvariant();
                            string dataLower = valData.ToLowerInvariant();

                            if (keywords.Any(k => nameLower.Contains(k) || dataLower.Contains(k)))
                            {
                                string rootName = root == Registry.CurrentUser ? "HKEY_CURRENT_USER" : "HKEY_LOCAL_MACHINE";
                                residuals.Add(new ResidualItem
                                {
                                    Path = $@"{rootName}\{path} -> {valueName}",
                                    Type = "Inicio de Windows",
                                    Size = 0
                                });
                            }
                        }
                    }
                }
                catch { }
            }

            var startupFolderPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
            };

            foreach (var folder in startupFolderPaths)
            {
                try
                {
                    if (!Directory.Exists(folder)) continue;
                    var files = Directory.GetFiles(folder);
                    foreach (var file in files)
                    {
                        string fileName = Path.GetFileName(file).ToLowerInvariant();
                        if (keywords.Any(k => fileName.Contains(k)))
                        {
                            long size = 0;
                            try { size = new FileInfo(file).Length; } catch { }
                            residuals.Add(new ResidualItem
                            {
                                Path = file,
                                Type = "Inicio de Windows",
                                Size = size
                            });
                        }
                    }
                }
                catch { }
            }
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
                            string[] parts = item.Path.Split('\\');
                            if (parts.Length > 2)
                            {
                                var rootKey = parts[0] == "HKEY_CURRENT_USER" ? Registry.CurrentUser : Registry.LocalMachine;
                                string subKeyPath = string.Join('\\', parts.Skip(1));
                                
                                rootKey.DeleteSubKeyTree(subKeyPath, false);
                                Log.Information("Clave de registro residual eliminada: {Path}", item.Path);
                            }
                        }
                        else if (item.Type == "Inicio de Windows")
                        {
                            if (item.Path.Contains(" -> "))
                            {
                                string[] split = item.Path.Split(new[] { " -> " }, StringSplitOptions.None);
                                string fullRegPath = split[0];
                                string valName = split[1];

                                string[] parts = fullRegPath.Split('\\');
                                if (parts.Length > 1)
                                {
                                    var rootKey = parts[0] == "HKEY_CURRENT_USER" ? Registry.CurrentUser : Registry.LocalMachine;
                                    string subKeyPath = string.Join('\\', parts.Skip(1));

                                    using (var key = rootKey.OpenSubKey(subKeyPath, true))
                                    {
                                        if (key != null && key.GetValue(valName) != null)
                                        {
                                            key.DeleteValue(valName, false);
                                            Log.Information("Valor de inicio residual eliminado del Registro: {Path}", item.Path);
                                        }
                                    }
                                }
                            }
                            else if (File.Exists(item.Path))
                            {
                                File.Delete(item.Path);
                                Log.Information("Acceso directo de inicio residual eliminado: {Path}", item.Path);
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
