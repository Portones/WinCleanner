using System.Collections.Generic;

namespace WinCleaner.Models
{
    public class CpuMetrics
    {
        public double UsagePercentage { get; set; }
        public string TemperatureText { get; set; } = "No disponible";
    }

    public class RamMetrics
    {
        public double TotalGb { get; set; }
        public double UsedGb { get; set; }
        public double AvailableGb { get; set; }
        public double UsagePercentage { get; set; }
        
        // Memoria Comprometida (Committed Memory)
        public double CommittedGb { get; set; }
        public double CommittedLimitGb { get; set; }
        
        // Descripciones para la interfaz
        public string PhysicalDescription => $"En uso: {UsedGb:F1} GB / Total: {TotalGb:F1} GB";
        public string CommittedDescription => $"Comprometida: {CommittedGb:F1} GB / Límite: {CommittedLimitGb:F1} GB";
    }

    public class GpuMetrics
    {
        public string Name { get; set; } = "GPU desconocida";
        public double UsagePercentage { get; set; }
        public string DriverVersion { get; set; } = string.Empty;
        public string AdapterRam { get; set; } = string.Empty;

        /// <summary>Texto corto para mostrar el nombre truncado.</summary>
        public string ShortName => Name.Length > 30 ? Name[..30] + "…" : Name;
    }

    public class DiskMetrics
    {
        public string DriveLetter { get; set; } = string.Empty;
        public string VolumeLabel { get; set; } = string.Empty;
        public double TotalSizeGb { get; set; }
        public double FreeSpaceGb { get; set; }
        public double UsedSpaceGb => TotalSizeGb - FreeSpaceGb;
        public double UsedPercentage => TotalSizeGb > 0 ? (UsedSpaceGb / TotalSizeGb) * 100 : 0;
        public string SmartStatus { get; set; } = "No disponible";
        public string DriveType { get; set; } = "Desconocido";

        public string Description => $"{DriveLetter} ({VolumeLabel}) - {UsedSpaceGb:F1} GB usados de {TotalSizeGb:F1} GB ({UsedPercentage:F0}%)";
    }

    public class SystemHealthMetrics
    {
        public CpuMetrics Cpu { get; set; } = new();
        public RamMetrics Ram { get; set; } = new();
        public List<DiskMetrics> Disks { get; set; } = new();
        public List<GpuMetrics> Gpus { get; set; } = new();
        public string OverallHealthStatus { get; set; } = "Calculando...";
        public string UptimeText { get; set; } = "Cargando...";
    }
}
