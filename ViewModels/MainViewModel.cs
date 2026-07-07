using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace WinCleaner.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentPage;
        private string _activePage = "Dashboard";
        private readonly DashboardViewModel _dashboardViewModel;
        private readonly CleanupViewModel _cleanupViewModel;
        private readonly DuplicateFilesViewModel _duplicateFilesViewModel;
        private readonly StartupViewModel _startupViewModel;
        private readonly ServicesViewModel _servicesViewModel;
        private readonly ContextMenuViewModel _contextMenuViewModel;
        private readonly UninstallerViewModel _uninstallerViewModel;
        private readonly DiskAnalyzerViewModel _diskAnalyzerViewModel;
        private readonly PerformanceViewModel _performanceViewModel;
        private readonly SettingsViewModel _settingsViewModel;

        public ViewModelBase CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public string ActivePage
        {
            get => _activePage;
            set => SetProperty(ref _activePage, value);
        }

        public ICommand NavigateCommand { get; }

        public MainViewModel(
            DashboardViewModel dashboardViewModel, 
            CleanupViewModel cleanupViewModel, 
            DuplicateFilesViewModel duplicateFilesViewModel,
            StartupViewModel startupViewModel,
            ServicesViewModel servicesViewModel,
            ContextMenuViewModel contextMenuViewModel,
            UninstallerViewModel uninstallerViewModel,
            DiskAnalyzerViewModel diskAnalyzerViewModel,
            PerformanceViewModel performanceViewModel,
            SettingsViewModel settingsViewModel)
        {
            _dashboardViewModel = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));
            _cleanupViewModel = cleanupViewModel ?? throw new ArgumentNullException(nameof(cleanupViewModel));
            _duplicateFilesViewModel = duplicateFilesViewModel ?? throw new ArgumentNullException(nameof(duplicateFilesViewModel));
            _startupViewModel = startupViewModel ?? throw new ArgumentNullException(nameof(startupViewModel));
            _servicesViewModel = servicesViewModel ?? throw new ArgumentNullException(nameof(servicesViewModel));
            _contextMenuViewModel = contextMenuViewModel ?? throw new ArgumentNullException(nameof(contextMenuViewModel));
            _uninstallerViewModel = uninstallerViewModel ?? throw new ArgumentNullException(nameof(uninstallerViewModel));
            _diskAnalyzerViewModel = diskAnalyzerViewModel ?? throw new ArgumentNullException(nameof(diskAnalyzerViewModel));
            _performanceViewModel = performanceViewModel ?? throw new ArgumentNullException(nameof(performanceViewModel));
            _settingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));

            // Configurar página de inicio por defecto
            _currentPage = _dashboardViewModel;

            NavigateCommand = new RelayCommand<string>(Navigate);
        }

        private void Navigate(string? destination)
        {
            if (string.IsNullOrEmpty(destination)) return;

            ActivePage = destination;

            if (destination.Equals("Dashboard", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _dashboardViewModel;
            }
            else if (destination.Equals("Cleanup", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _cleanupViewModel;
            }
            else if (destination.Equals("Duplicates", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _duplicateFilesViewModel;
            }
            else if (destination.Equals("Startup", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _startupViewModel;
            }
            else if (destination.Equals("Services", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _servicesViewModel;
            }
            else if (destination.Equals("ContextMenu", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _contextMenuViewModel;
            }
            else if (destination.Equals("Uninstaller", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _uninstallerViewModel;
            }
            else if (destination.Equals("DiskAnalyzer", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _diskAnalyzerViewModel;
            }
            else if (destination.Equals("Performance", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _performanceViewModel;
            }
            else if (destination.Equals("Settings", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _settingsViewModel;
            }
        }
    }
}
