using System;
using System.Collections.Generic;

namespace WinCleaner.Models
{
    public class AppSettings
    {
        public string Theme { get; set; } = "Dark"; // "Dark" or "Light"
        public string Language { get; set; } = "es"; // "es" or "en"
        public bool BypassRecycleBin { get; set; } = false; // Requiere confirmación doble si es true
        public bool StartWithWindows { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public bool EnableBackgroundMonitoring { get; set; } = true;

        // Notificaciones nativas
        public bool NotifyHighRam { get; set; } = true;
        public bool NotifyLowDiskSpace { get; set; } = true;
        public bool NotifyHighTemp { get; set; } = true;
        
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

        // Configuración de mantenimiento automático programado
        public bool MaintenanceTaskEnabled { get; set; } = false;
        public string MaintenanceFrequency { get; set; } = "weekly"; // weekly, monthly, onlogon, idle
        public string MaintenanceDay { get; set; } = "SUN"; // SUN, MON, etc. o número de día del mes
        public string MaintenanceTime { get; set; } = "03:00";

        // Backup de registros de inicio desactivados
        public List<StartupRegistryBackup> DisabledStartupRegistryApps { get; set; } = new();

        // Historial de limpiezas realizadas
        public List<CleanupHistoryItem> CleanupHistory { get; set; } = new();
    }

    public class CleanupRegistryBackup
    {
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string RegistryPath { get; set; } = string.Empty; // "HKCU" o "HKLM"
    }

    public class CleanupHistoryItem
    {
        public DateTime DateTime { get; set; } = DateTime.Now;
        public long BytesCleaned { get; set; }
        public int ItemsCount { get; set; }

        public string SizeText => CleanableItem.FormatSize(BytesCleaned);
        public string DateText => DateTime.ToString("dd/MM/yyyy HH:mm");
    }

    public class StartupRegistryBackup
    {
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string RegistryPath { get; set; } = string.Empty; // "HKCU" o "HKLM"
    }
}
