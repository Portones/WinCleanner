using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WinCleaner.ViewModels.Categories;

namespace WinCleaner.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentPage;
        private string _activePage = "Dashboard";

        private readonly DashboardViewModel _dashboardViewModel;
        private readonly DiagnosticsCategoryViewModel _diagnosticsCategoryViewModel;
        private readonly CleanupCategoryViewModel _cleanupCategoryViewModel;
        private readonly DiskCategoryViewModel _diskCategoryViewModel;
        private readonly AppCategoryViewModel _appCategoryViewModel;
        private readonly OptimizationCategoryViewModel _optimizationCategoryViewModel;
        private readonly SettingsViewModel _settingsViewModel;

        // Referencias a ViewModels individuales para inicio/paro de temporizadores
        private readonly RamOptimizerViewModel _ramOptimizerViewModel;
        private readonly TemperatureViewModel _temperatureViewModel;
        private readonly BatteryViewModel _batteryViewModel;

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
            DiagnosticsCategoryViewModel diagnosticsCategoryViewModel,
            CleanupCategoryViewModel cleanupCategoryViewModel,
            DiskCategoryViewModel diskCategoryViewModel,
            AppCategoryViewModel appCategoryViewModel,
            OptimizationCategoryViewModel optimizationCategoryViewModel,
            SettingsViewModel settingsViewModel,
            RamOptimizerViewModel ramOptimizerViewModel,
            TemperatureViewModel temperatureViewModel,
            BatteryViewModel batteryViewModel)
        {
            _dashboardViewModel = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));
            _diagnosticsCategoryViewModel = diagnosticsCategoryViewModel ?? throw new ArgumentNullException(nameof(diagnosticsCategoryViewModel));
            _cleanupCategoryViewModel = cleanupCategoryViewModel ?? throw new ArgumentNullException(nameof(cleanupCategoryViewModel));
            _diskCategoryViewModel = diskCategoryViewModel ?? throw new ArgumentNullException(nameof(diskCategoryViewModel));
            _appCategoryViewModel = appCategoryViewModel ?? throw new ArgumentNullException(nameof(appCategoryViewModel));
            _optimizationCategoryViewModel = optimizationCategoryViewModel ?? throw new ArgumentNullException(nameof(optimizationCategoryViewModel));
            _settingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));
            _ramOptimizerViewModel = ramOptimizerViewModel ?? throw new ArgumentNullException(nameof(ramOptimizerViewModel));
            _temperatureViewModel = temperatureViewModel ?? throw new ArgumentNullException(nameof(temperatureViewModel));
            _batteryViewModel = batteryViewModel ?? throw new ArgumentNullException(nameof(batteryViewModel));

            // Configurar página de inicio por defecto
            _currentPage = _dashboardViewModel;

            NavigateCommand = new RelayCommand<string>(Navigate);
        }

        public SettingsViewModel SettingsViewModel => _settingsViewModel;

        private void Navigate(string? destination)
        {
            if (string.IsNullOrEmpty(destination)) return;

            // Gestión de temporizadores al cambiar de categoría
            if (ActivePage.Equals("DiagnosticsCategory", StringComparison.OrdinalIgnoreCase))
            {
                _temperatureViewModel.StopTimer();
                _batteryViewModel.StopTimer();
            }
            else if (ActivePage.Equals("OptimizationCategory", StringComparison.OrdinalIgnoreCase))
            {
                _ramOptimizerViewModel.StopTimer();
            }

            ActivePage = destination;

            if (destination.Equals("Dashboard", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _dashboardViewModel;
            }
            else if (destination.Equals("DiagnosticsCategory", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("Temperature", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("Battery", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("Drivers", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("CrashInspector", StringComparison.OrdinalIgnoreCase))
            {
                _temperatureViewModel.StartTimer();
                _batteryViewModel.StartTimer();
                ActivePage = "DiagnosticsCategory";
                CurrentPage = _diagnosticsCategoryViewModel;
            }
            else if (destination.Equals("CleanupCategory", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("Cleanup", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("Duplicates", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("PhotosCleanup", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("BrowserCleanup", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("CleanupHistory", StringComparison.OrdinalIgnoreCase))
            {
                _cleanupCategoryViewModel.CleanupHistoryViewModel.LoadHistory();
                ActivePage = "CleanupCategory";
                CurrentPage = _cleanupCategoryViewModel;
            }
            else if (destination.Equals("DiskCategory", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("DiskAnalyzer", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("SsdOptimizer", StringComparison.OrdinalIgnoreCase))
            {
                ActivePage = "DiskCategory";
                CurrentPage = _diskCategoryViewModel;
            }
            else if (destination.Equals("AppCategory", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("Uninstaller", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("Updater", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("RuntimeInstaller", StringComparison.OrdinalIgnoreCase))
            {
                ActivePage = "AppCategory";
                CurrentPage = _appCategoryViewModel;
            }
            else if (destination.Equals("OptimizationCategory", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("Performance", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("TcpTweaker", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("SystemRepair", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("RamOptimizer", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("Startup", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("Services", StringComparison.OrdinalIgnoreCase) ||
                     destination.Equals("ContextMenu", StringComparison.OrdinalIgnoreCase))
            {
                _ramOptimizerViewModel.StartTimer();
                ActivePage = "OptimizationCategory";
                CurrentPage = _optimizationCategoryViewModel;
            }
            else if (destination.Equals("Settings", StringComparison.OrdinalIgnoreCase))
            {
                CurrentPage = _settingsViewModel;
            }
        }
    }
}
