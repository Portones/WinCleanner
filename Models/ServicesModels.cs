using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCleaner.Models
{
    public class ServiceItem : ObservableObject
    {
        private string _status = "Detenido";
        private string _startType = "Manual";
        private bool _isRunning;

        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string StartType
        {
            get => _startType;
            set => SetProperty(ref _startType, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    Status = value ? "En ejecución" : "Detenido";
                }
            }
        }

        public bool CanStop { get; set; }
        public bool IsCritical { get; set; }
    }
}
