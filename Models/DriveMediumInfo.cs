using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCleaner.Models
{
    public class DriveMediumInfo : ObservableObject
    {
        private string _status = "Listo para optimizar";

        public string DriveLetter { get; set; } = string.Empty;
        public string VolumeLabel { get; set; } = string.Empty;
        public string DriveType { get; set; } = "SSD";
        public bool IsSsd { get; set; } = true;
        public long TotalSize { get; set; }
        public long FreeSpace { get; set; }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string TotalSizeText => CleanableItem.FormatSize(TotalSize);
        public string FreeSpaceText => CleanableItem.FormatSize(FreeSpace);
    }
}
