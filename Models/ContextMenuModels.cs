using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCleaner.Models
{
    public class ContextMenuItem : ObservableObject
    {
        private bool _isEnabled;

        public string Name { get; set; } = string.Empty;
        public string RegistryPath { get; set; } = string.Empty; // Ruta de registro de la clave
        public string RegistryKeyName { get; set; } = string.Empty; // Nombre exacto de la subclave
        public string HandlerGuid { get; set; } = string.Empty;  // GUID de la clase del handler
        public string Category { get; set; } = string.Empty;      // "Archivos", "Carpetas", "Global"

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
    }
}
