using System;

namespace WinCleaner.Models
{
    public class DriverItem
    {
        public string DeviceName { get; set; } = string.Empty;
        public string DriverVersion { get; set; } = string.Empty;
        public string DriverProvider { get; set; } = string.Empty;
        public string DriverDateString { get; set; } = string.Empty;
        public DateTime DriverDate { get; set; }
        public string DeviceClass { get; set; } = string.Empty;
        public string Signer { get; set; } = string.Empty;
        public bool IsOutdated { get; set; }

        public string StatusColor => IsOutdated ? "#F59E0B" : "#10B981"; // Ámbar si es antiguo (> 3 años), Verde si está al día
        public string StatusText => IsOutdated ? "Antiguo" : "Al día";
    }
}
