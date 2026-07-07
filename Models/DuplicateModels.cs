using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace WinCleaner.Models
{
    public class DuplicateFile : ObservableObject
    {
        private bool _isSelected;

        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime LastModified { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string SizeText => CleanableItem.FormatSize(Size);
    }

    public class DuplicateGroup
    {
        public string Hash { get; set; } = string.Empty;
        public long Size { get; set; }
        public List<DuplicateFile> Files { get; set; } = new();

        public string SizeText => CleanableItem.FormatSize(Size);
        public string GroupTitle => $"Grupo: {Files[0].Name} ({Files.Count} copias - {SizeText} cada una)";
    }
}
