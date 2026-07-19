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
    public class RegistryAppScanner : IRegistryAppScanner
    {
        private static readonly HashSet<string> KnownBloatware = new(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.549981C3F5F10", // Cortana
            "Microsoft.GetHelp",
            "Microsoft.Getstarted",
            "Microsoft.Messaging",
            "Microsoft.MixedReality.Portal",
            "Microsoft.OneConnect",
            "Microsoft.People",
            "Microsoft.SkypeApp",
            "Microsoft.Xbox.TCUI",
            "Microsoft.XboxApp",
            "Microsoft.XboxGameOverlay",
            "Microsoft.XboxGamingOverlay",
            "Microsoft.XboxIdentityProvider",
            "Microsoft.XboxSpeechToTextOverlay",
            "Microsoft.YourPhone",
            "Microsoft.ZuneMusic",
            "Microsoft.ZuneVideo",
            "Microsoft.MicrosoftSolitaireCollection",
            "Microsoft.BingNews",
            "Microsoft.BingWeather",
            "Microsoft.MicrosoftOfficeHub",
            "Microsoft.BingSearch",
            "Microsoft.WindowsFeedbackHub",
            "Microsoft.WindowsMaps",
            "Microsoft.StickyNotes",
            "Microsoft.Office.OneNote"
        };

        public async Task<List<InstalledApp>> ScanInstalledAppsAsync(CancellationToken cancellationToken)
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
                                InstallDate = installDate,
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

                    // Omitir componentes nativos esenciales de Windows (a menos que sean bloatware conocido)
                    bool isBloatware = KnownBloatware.Contains(pkg.Id.Name);
                    if (!isBloatware && (pkg.SignatureKind == Windows.ApplicationModel.PackageSignatureKind.System || 
                        pkg.Id.PublisherId == "cw5n1h2txyewy")) // Clave de firmas nativas de Microsoft
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
                        InstallDate = installDate,
                        IsUwp = true,
                        IsBloatware = isBloatware
                    });
                }
                catch { }
            }
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
