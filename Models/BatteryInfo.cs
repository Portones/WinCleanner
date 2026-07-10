namespace WinCleaner.Models
{
    public class BatteryInfo
    {
        public bool HasBattery { get; set; }
        public double ChargePercentage { get; set; }
        public bool IsCharging { get; set; }
        public string EstimatedTimeRemainingText { get; set; } = "Desconocido";
        public double DesignedCapacity { get; set; }
        public double FullChargedCapacity { get; set; }
        public double WearLevel { get; set; }
        public string BatteryHealthState { get; set; } = "Desconocido";
        
        public string StatusColor => BatteryHealthState switch
        {
            "Excelente" => "#10B981", // Verde Esmeralda
            "Bueno" => "#818CF8",     // Azul Índigo
            "Reemplazo Recomendado" => "#F59E0B", // Ámbar
            "Crítico" => "#EF4444",   // Rojo
            _ => "#64748B"            // Gris
        };

        public string ChargeStatusText => IsCharging ? "Cargando" : "Descargando";
    }
}
