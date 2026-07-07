using System;
using CommunityToolkit.Mvvm.ComponentModel;
using WinCleaner.Models;

namespace WinCleaner.Models
{
    public class InstalledApp
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public DateTime? InstallDate { get; set; }
        public string DisplayVersion { get; set; } = string.Empty;
        public long EstimatedSize { get; set; }
        public string UninstallString { get; set; } = string.Empty;
        public string InstallLocation { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public bool IsUwp { get; set; }
        public string PackageFullName { get; set; } = string.Empty;

        public string SizeText => EstimatedSize > 0 ? CleanableItem.FormatSize(EstimatedSize) : "Desconocido";
        public string InstallDateText => InstallDate.HasValue ? InstallDate.Value.ToString("dd/MM/yyyy") : "Desconocido";
    }

    public class ResidualItem : ObservableObject
    {
        private bool _isSelected = true;

        public string Path { get; set; } = string.Empty;
        public string Type { get; set; } = "Carpeta"; // "Carpeta", "Archivo", "Registro"
        public long Size { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string SizeText => Size > 0 ? CleanableItem.FormatSize(Size) : "N/A";
    }
}
