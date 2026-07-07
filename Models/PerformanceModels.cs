using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCleaner.Models
{
    public class DnsServerItem : ObservableObject
    {
        private long? _latencyMs;
        private string _status = "Sin probar";
        private bool _isActive;

        public string Name { get; set; } = string.Empty;
        public string PrimaryIp { get; set; } = string.Empty;
        public string SecondaryIp { get; set; } = string.Empty;

        public long? LatencyMs
        {
            get => _latencyMs;
            set
            {
                if (SetProperty(ref _latencyMs, value))
                {
                    OnPropertyChanged(nameof(LatencyText));
                    OnPropertyChanged(nameof(LatencyProgressValue));
                }
            }
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public string LatencyText
        {
            get
            {
                if (!LatencyMs.HasValue) return "Pendiente";
                if (LatencyMs.Value >= 999) return "Error/T.O.";
                return $"{LatencyMs.Value} ms";
            }
        }

        public double LatencyProgressValue
        {
            get
            {
                if (!LatencyMs.HasValue) return 0;
                // Escalar latencia (0ms - 200ms) para una barra de progreso de 100%
                double val = (double)LatencyMs.Value;
                if (val > 200) return 100;
                return (val / 200.0) * 100.0;
            }
        }
    }
}
