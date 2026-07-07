using System;
using System.Collections.Generic;

namespace WinCleaner.Models
{
    public class AppSettings
    {
        public string Theme { get; set; } = "Dark"; // "Dark" or "Light"
        public string Language { get; set; } = "es"; // "es" or "en"
        public bool BypassRecycleBin { get; set; } = false; // Requiere confirmación doble si es true
        
        // Configuración de Archivos Grandes
        public long MinLargeFileSizeMb { get; set; } = 100; // Por defecto 100MB
        
        // Directorios a excluir (Lista negra inicial de seguridad)
        public List<string> ExcludedDirectories { get; set; } = new()
        {
            @"C:\Windows\System32",
            @"C:\Windows\WinSxS",
            @"C:\Windows\SysWOW64",
            @"C:\Windows\Microsoft.NET",
            @"C:\Windows\Boot",
            @"C:\Windows\diagnostics"
        };

        // Directorios personalizados para analizar
        public List<string> CustomScanDirectories { get; set; } = new();

        // Configuración de análisis automáticos
        public bool EnableScheduledScan { get; set; } = false;
        public string ScanFrequency { get; set; } = "Weekly"; // Daily, Weekly, Monthly
        public TimeSpan ScheduledScanTime { get; set; } = new TimeSpan(12, 0, 0); // 12:00 PM

        // Backup de registros de inicio desactivados
        public List<StartupRegistryBackup> DisabledStartupRegistryApps { get; set; } = new();
    }

    public class StartupRegistryBackup
    {
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string RegistryPath { get; set; } = string.Empty; // "HKCU" o "HKLM"
    }
}
