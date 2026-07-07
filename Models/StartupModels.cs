using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCleaner.Models
{
    public class StartupApp : ObservableObject
    {
        private bool _isEnabled;

        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty; // e.g. "HKCU Run", "HKLM Run", "Carpeta de Inicio"
        public string LocationType { get; set; } = string.Empty; // "RegistryHKCU", "RegistryHKLM", "FolderUser", "FolderCommon"
        public string FilePath { get; set; } = string.Empty; // Ruta del acceso directo si aplica
        public string RegistryValueName { get; set; } = string.Empty; // Nombre del valor de registro si aplica

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
    }
}
