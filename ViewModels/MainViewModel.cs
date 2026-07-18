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
        private readonly UpdaterViewModel _updaterViewModel;
        private readonly PhotosCleanupViewModel _photosCleanupViewModel;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly RamOptimizerViewModel _ramOptimizerViewModel;
        private readonly TemperatureViewModel _temperatureViewModel;
        private readonly BatteryViewModel _batteryViewModel;
        private readonly DriverViewModel _driverViewModel;
        private readonly BrowserCleanupViewModel _browserCleanupViewModel;
        private readonly SystemRepairViewModel _systemRepairViewModel;
        private readonly CrashInspectorViewModel _crashInspectorViewModel;

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
            UpdaterViewModel updaterViewModel,
            PhotosCleanupViewModel photosCleanupViewModel,
            SettingsViewModel settingsViewModel,
            RamOptimizerViewModel ramOptimizerViewModel,
            TemperatureViewModel temperatureViewModel,
            BatteryViewModel batteryViewModel,
            DriverViewModel driverViewModel,
            BrowserCleanupViewModel browserCleanupViewModel,
            SystemRepairViewModel systemRepairViewModel,
            CrashInspectorViewModel crashInspectorViewModel)
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
            _updaterViewModel = updaterViewModel ?? throw new ArgumentNullException(nameof(updaterViewModel));
            _photosCleanupViewModel = photosCleanupViewModel ?? throw new ArgumentNullException(nameof(photosCleanupViewModel));
            _settingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));
            _ramOptimizerViewModel = ramOptimizerViewModel ?? throw new ArgumentNullException(nameof(ramOptimizerViewModel));
            _temperatureViewModel = temperatureViewModel ?? throw new ArgumentNullException(nameof(temperatureViewModel));
            _batteryViewModel = batteryViewModel ?? throw new ArgumentNullException(nameof(batteryViewModel));
            _driverViewModel = driverViewModel ?? throw new ArgumentNullException(nameof(driverViewModel));
            _browserCleanupViewModel = browserCleanupViewModel ?? throw new ArgumentNullException(nameof(browserCleanupViewModel));
            _systemRepairViewModel = systemRepairViewModel ?? throw new ArgumentNullException(nameof(systemRepairViewModel));
            _crashInspectorViewModel = crashInspectorViewModel ?? throw new ArgumentNullException(nameof(crashInspectorViewModel));

            // Configurar página de inicio por defecto
            _currentPage = _dashboardViewModel;

            NavigateCommand = new RelayCommand<string>(Navigate);
        }

        public SettingsViewModel SettingsViewModel => _settingsViewModel;

        private void Navigate(string? destination)
        {
            if (string.IsNullOrEmpty(destination)) return;

            // Detener refresco automático de procesos si salimos de Optimizar RAM
            if (ActivePage.Equals("RamOptimizer", StringComparison.OrdinalIgnoreCase))
            {
                _ramOptimizerViewModel.StopTimer();
            }
            else if (ActivePage.Equals("Temperature", StringComparison.OrdinalIgnoreCase))
            {
                _temperatureViewModel.StopTimer();
            }
            else if (ActivePage.Equals("Battery", StringComparison.OrdinalIgnoreCase))
            {
                _batteryViewModel.StopTimer();
            }

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
            else if (destination.Equals("Updater", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _updaterViewModel;
            }
            else if (destination.Equals("PhotosCleanup", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _photosCleanupViewModel;
            }
            else if (destination.Equals("RamOptimizer", StringComparison.OrdinalIgnoreCase))
            {
                _ramOptimizerViewModel.StartTimer();
                CurrentPage = _ramOptimizerViewModel;
            }
            else if (destination.Equals("Temperature", StringComparison.OrdinalIgnoreCase))
            {
                _temperatureViewModel.StartTimer();
                CurrentPage = _temperatureViewModel;
            }
            else if (destination.Equals("Battery", StringComparison.OrdinalIgnoreCase))
            {
                _batteryViewModel.StartTimer();
                CurrentPage = _batteryViewModel;
            }
            else if (destination.Equals("Drivers", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _driverViewModel;
            }
            else if (destination.Equals("Settings", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _settingsViewModel;
            }
            else if (destination.Equals("BrowserCleanup", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _browserCleanupViewModel;
            }
            else if (destination.Equals("SystemRepair", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _systemRepairViewModel;
            }
            else if (destination.Equals("CrashInspector", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _crashInspectorViewModel;
            }
        }
    }
}
