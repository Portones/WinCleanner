using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace WinCleaner.Models
{
    public class CleanableItem : ObservableObject
    {
        private bool _isSelected = true; // Por defecto todo seleccionado para limpieza

        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public string FileType { get; set; } = "Archivo Temporal";
        public string ModuleId { get; set; } = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string SizeText => FormatSize(Size);

        public static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "0 Bytes";
            string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB" };
            int counter = 0;
            double number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:F1} {suffixes[counter]}";
        }
    }

    public class ScanResult
    {
        public List<CleanableItem> Items { get; set; } = new();
        public long TotalSize { get; set; }
        public int FilesCount => Items.Count;
    }
}
