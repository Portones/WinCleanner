namespace WinCleaner.Models
{
    public class TemperatureItem
    {
        public string ComponentName { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public string ValueText => CurrentValue > 0 ? $"{CurrentValue:F0}°C" : "No disponible";
        public string Status { get; set; } = "Normal"; // Normal, Templado, Caliente (Crítico)
        public string StatusColor => Status switch
        {
            "Caliente" => "#EF4444", // Rojo
            "Templado" => "#F59E0B", // Ámbar
            _ => "#10B981"           // Verde Esmeralda
        };
    }
}
