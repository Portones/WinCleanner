using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCleaner.Models
{
    public class RuntimeItem : ObservableObject
    {
        private bool _isSelected;
        private string _status = "Pendiente";

        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "Runtimes";

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
    }
}
