using System;
using System.Collections.Generic;
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
    public class StartupManagerService : IStartupManagerService
    {
        private readonly IConfigurationService _configurationService;
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public StartupManagerService(IConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        public async Task<List<StartupApp>> GetStartupAppsAsync(CancellationToken cancellationToken)
        {
            var apps = new List<StartupApp>();

            // 1. HKCU Run Registry (Activos)
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key != null)
                    {
                        foreach (var valueName in key.GetValueNames())
                        {
                            var val = key.GetValue(valueName);
                            if (val != null)
                            {
                                apps.Add(new StartupApp
                                {
                                    Name = valueName,
                                    Command = val.ToString() ?? string.Empty,
                                    Location = "Registro (Usuario)",
                                    LocationType = "RegistryHKCU",
                                    RegistryValueName = valueName,
                                    IsEnabled = true
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al leer inicio en HKCU.");
            }

            // 2. HKLM Run Registry (Activos)
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(RunKeyPath, false))
                {
                    if (key != null)
                    {
                        foreach (var valueName in key.GetValueNames())
                        {
                            var val = key.GetValue(valueName);
                            if (val != null)
                            {
                                apps.Add(new StartupApp
                                {
                                    Name = valueName,
                                    Command = val.ToString() ?? string.Empty,
                                    Location = "Registro (Máquina)",
                                    LocationType = "RegistryHKLM",
                                    RegistryValueName = valueName,
                                    IsEnabled = true
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error al leer inicio en HKLM (puede requerir permisos elevados): {Msg}", ex.Message);
            }

            // 3. Registry Desactivados (Backup de WinCleaner)
            cancellationToken.ThrowIfCancellationRequested();
            var disabledRegistry = _configurationService.CurrentSettings.DisabledStartupRegistryApps;
            foreach (var backup in disabledRegistry)
            {
                apps.Add(new StartupApp
                {
                    Name = backup.Name,
                    Command = backup.Command,
                    Location = backup.RegistryPath == "HKCU" ? "Registro (Usuario - Desactivado)" : "Registro (Máquina - Desactivado)",
                    LocationType = backup.RegistryPath == "HKCU" ? "RegistryHKCU" : "RegistryHKLM",
                    RegistryValueName = backup.Name,
                    IsEnabled = false
                });
            }

            // 4. Carpeta de Inicio del Usuario (HKCU Startup Folder)
            cancellationToken.ThrowIfCancellationRequested();
            var userStartupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            await AddStartupFolderAppsAsync(userStartupPath, "Inicio (Usuario)", "FolderUser", apps, cancellationToken);

            // 5. Carpeta de Inicio Común (HKLM Startup Folder)
            cancellationToken.ThrowIfCancellationRequested();
            var commonStartupPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
            await AddStartupFolderAppsAsync(commonStartupPath, "Inicio (Común)", "FolderCommon", apps, cancellationToken);

            foreach (var app in apps)
            {
                PopulateStartupImpact(app);
            }

            return apps;
        }

        private async Task AddStartupFolderAppsAsync(string folderPath, string locationText, string locationType, List<StartupApp> apps, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(folderPath)) return;

            try
            {
                var files = Directory.GetFiles(folderPath, "*.*");
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    
                    bool isLnk = ext == ".lnk";
                    bool isDisabledLnk = ext == ".disabled" && file.EndsWith(".lnk.disabled", StringComparison.OrdinalIgnoreCase);

                    if (isLnk || isDisabledLnk)
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        if (isDisabledLnk)
                        {
                            name = Path.GetFileNameWithoutExtension(name); // Quita el .lnk de file.lnk.disabled
                        }

                        // Resolver destino del acceso directo
                        string target = file;
                        if (isLnk || file.EndsWith(".lnk.disabled"))
                        {
                            target = await Task.Run(() => ResolveShortcutTarget(file), cancellationToken);
                        }

                        apps.Add(new StartupApp
                        {
                            Name = name,
                            Command = target,
                            Location = locationText,
                            LocationType = locationType,
                            FilePath = file,
                            IsEnabled = isLnk
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al escanear carpeta de inicio: {Path}", folderPath);
            }
        }

        public async Task<bool> ToggleStartupAppAsync(StartupApp app, bool enable, CancellationToken cancellationToken)
        {
            if (app == null) return false;

            if (app.LocationType == "RegistryHKCU" || app.LocationType == "RegistryHKLM")
            {
                var isHKCU = app.LocationType == "RegistryHKCU";
                var rootKey = isHKCU ? Registry.CurrentUser : Registry.LocalMachine;
                var pathLabel = isHKCU ? "HKCU" : "HKLM";

                if (!enable) // Desactivar: Quitar del registro, guardar en configuración
                {
                    using (var key = rootKey.OpenSubKey(RunKeyPath, true))
                    {
                        if (key != null)
                        {
                            var val = key.GetValue(app.RegistryValueName);
                            if (val != null)
                            {
                                // Agregar al backup de configuración
                                var backup = new StartupRegistryBackup
                                {
                                    Name = app.RegistryValueName,
                                    Command = val.ToString() ?? string.Empty,
                                    RegistryPath = pathLabel
                                };
                                
                                _configurationService.CurrentSettings.DisabledStartupRegistryApps.Add(backup);
                                _configurationService.SaveSettings();

                                // Eliminar del registro
                                key.DeleteValue(app.RegistryValueName);
                                return true;
                            }
                        }
                    }
                }
                else // Activar: Leer de configuración, escribir en registro
                {
                    var backup = _configurationService.CurrentSettings.DisabledStartupRegistryApps
                        .FirstOrDefault(x => x.Name == app.RegistryValueName && x.RegistryPath == pathLabel);

                    if (backup != null)
                    {
                        using (var key = rootKey.OpenSubKey(RunKeyPath, true))
                        {
                            if (key != null)
                            {
                                // Escribir en registro
                                key.SetValue(backup.Name, backup.Command);

                                // Quitar del backup
                                _configurationService.CurrentSettings.DisabledStartupRegistryApps.Remove(backup);
                                _configurationService.SaveSettings();
                                return true;
                            }
                        }
                    }
                }
            }
            else if (app.LocationType == "FolderUser" || app.LocationType == "FolderCommon")
            {
                if (!File.Exists(app.FilePath)) return false;

                if (!enable) // Desactivar: Cambiar nombre de .lnk a .lnk.disabled
                {
                    if (app.FilePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                    {
                        string newPath = app.FilePath + ".disabled";
                        File.Move(app.FilePath, newPath);
                        app.FilePath = newPath;
                        return true;
                    }
                }
                else // Activar: Cambiar nombre de .lnk.disabled a .lnk
                {
                    if (app.FilePath.EndsWith(".lnk.disabled", StringComparison.OrdinalIgnoreCase))
                    {
                        string newPath = app.FilePath.Substring(0, app.FilePath.Length - ".disabled".Length);
                        File.Move(app.FilePath, newPath);
                        app.FilePath = newPath;
                        return true;
                    }
                }
            }

            return false;
        }

#pragma warning disable CS8602
        private static string ResolveShortcutTarget(string shortcutPath)
        {
            try
            {
                Type shellAppType = Type.GetTypeFromProgID("Shell.Application")!;
                dynamic shell = Activator.CreateInstance(shellAppType)!;
                string directory = Path.GetDirectoryName(shortcutPath)!;
                string file = Path.GetFileName(shortcutPath);
                dynamic folder = shell.NameSpace(directory);
                if (folder != null)
                {
                    dynamic folderItem = folder.ParseName(file);
                    if (folderItem != null && folderItem.IsLink)
                    {
                        dynamic link = folderItem.GetLink;
                        if (link != null)
                        {
                            return link.Path;
                        }
                    }
                }
            }
            catch
            {
                // Fallback
            }
            return shortcutPath;
        }
#pragma warning restore CS8602

        private void PopulateStartupImpact(StartupApp app)
        {
            if (app == null) return;

            string cmd = app.Command.ToLowerInvariant();
            string name = app.Name.ToLowerInvariant();

            // Criterios para impacto Alto
            var highKeywords = new[]
            {
                "electron", "discord", "spotify", "teams", "slack", "steam", "epicgames", 
                "gog", "onedrive", "dropbox", "adobe", "creative cloud", "office", "msedge", 
                "chrome", "firefox", "browser", "update", "telemetry", "assistant"
            };

            // Criterios para impacto Bajo
            var lowKeywords = new[]
            {
                "rtkaudioservice", "realtek", "synaptics", "windows defender", "securityhealth", 
                "sound", "audio", "driver", "intel", "amd", "nvidia", "display", "keys", "hotkey"
            };

            if (highKeywords.Any(k => cmd.Contains(k) || name.Contains(k)))
            {
                app.Impact = "Alto";
                app.ImpactColor = "#EF4444"; // Rojo
            }
            else if (lowKeywords.Any(k => cmd.Contains(k) || name.Contains(k)))
            {
                app.Impact = "Bajo";
                app.ImpactColor = "#10B981"; // Verde
            }
            else
            {
                app.Impact = "Medio";
                app.ImpactColor = "#F59E0B"; // Amarillo
            }

            // Opcional: si el archivo ejecutable existe en el disco, comprobar su tamaño
            try
            {
                string cleanPath = app.Command.Trim(' ', '"');
                if (cleanPath.Contains(" -") || cleanPath.Contains(" /"))
                {
                    int index = cleanPath.IndexOf(" -");
                    if (index > 0) cleanPath = cleanPath.Substring(0, index).Trim();
                    index = cleanPath.IndexOf(" /");
                    if (index > 0) cleanPath = cleanPath.Substring(0, index).Trim();
                }

                if (File.Exists(cleanPath))
                {
                    var fileInfo = new FileInfo(cleanPath);
                    if (fileInfo.Length > 40 * 1024 * 1024) // Mayor a 40MB
                    {
                        app.Impact = "Alto";
                        app.ImpactColor = "#EF4444";
                    }
                    else if (fileInfo.Length < 1.5 * 1024 * 1024 && app.Impact == "Medio") // Menor a 1.5MB y era Medio
                    {
                        app.Impact = "Bajo";
                        app.ImpactColor = "#10B981";
                    }
                }
            }
            catch { /* Ignorar errores de acceso a disco */ }
        }

        public void SetWindowsAutoStart(bool enable, bool minimized)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                            string command = $"\"{exePath}\"";
                            if (minimized)
                            {
                                command += " --minimized";
                            }
                            key.SetValue("WinCleaner", command);
                        }
                        else
                        {
                            if (key.GetValue("WinCleaner") != null)
                            {
                                key.DeleteValue("WinCleaner", false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al configurar el inicio automático con Windows en el Registro.");
            }
        }
    }
}
