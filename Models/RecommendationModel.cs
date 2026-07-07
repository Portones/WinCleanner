namespace WinCleaner.Models
{
    public class OptimizationRecommendation
    {
        public string Text { get; set; } = string.Empty;
        public string ActionText { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty; // "Reboot", "OptimizeRam", "NavigateCleanup"
        public string ColorHex { get; set; } = "#F59E0B";      // Naranja por defecto (Advertencia)
        public string IconPathData { get; set; } = string.Empty; // Datos de Path SVG para dibujar el icono
    }
}
