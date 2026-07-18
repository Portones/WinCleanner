using CommunityToolkit.Mvvm.ComponentModel;
using WinCleaner.Models;

namespace WinCleaner.Models
{
    public class HistoryBarItem : ObservableObject
    {
        public string DateLabel { get; set; } = string.Empty;
        public long BytesCleaned { get; set; }
        public double HeightValue { get; set; } = 10;
        public string FormattedSize => CleanableItem.FormatSize(BytesCleaned);
        public string ToolTipText => $"{DateLabel}: {FormattedSize} liberados";
    }
}
