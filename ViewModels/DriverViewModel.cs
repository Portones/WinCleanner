using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.ViewModels
{
    public class DriverViewModel : ViewModelBase
    {
        private readonly IDriverService _driverService;

        private ObservableCollection<DriverItem> _drivers = new();
        private ObservableCollection<DriverItem> _filteredDrivers = new();
        private ObservableCollection<DriverUpdateItem> _availableUpdates = new();
        
        private string _searchText = string.Empty;
        private string _selectedCategory = "TODOS";
        private int _totalDriversCount;
        private int _outdatedDriversCount;
        private int _availableUpdatesCount;
        private bool _isLoading;
        private string _statusMessage = "Listo";

        public ObservableCollection<DriverItem> Drivers
        {
            get => _drivers;
            set
            {
                if (SetProperty(ref _drivers, value))
                {
                    ApplyFilters();
                }
            }
        }

        public ObservableCollection<DriverItem> FilteredDrivers
        {
            get => _filteredDrivers;
            set => SetProperty(ref _filteredDrivers, value);
        }

        public ObservableCollection<DriverUpdateItem> AvailableUpdates
        {
            get => _availableUpdates;
            set => SetProperty(ref _availableUpdates, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    ApplyFilters();
                }
            }
        }

        public int TotalDriversCount
        {
            get => _totalDriversCount;
            set => SetProperty(ref _totalDriversCount, value);
        }

        public int OutdatedDriversCount
        {
            get => _outdatedDriversCount;
            set => SetProperty(ref _outdatedDriversCount, value);
        }

        public int AvailableUpdatesCount
        {
            get => _availableUpdatesCount;
            set
            {
                if (SetProperty(ref _availableUpdatesCount, value))
                {
                    OnPropertyChanged(nameof(HasAvailableUpdates));
                }
            }
        }

        public bool HasAvailableUpdates => AvailableUpdatesCount > 0;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private string _driverSuggestionText = string.Empty;
        private bool _showDriverSuggestion;

        public string DriverSuggestionText
        {
            get => _driverSuggestionText;
            set => SetProperty(ref _driverSuggestionText, value);
        }

        public bool ShowDriverSuggestion
        {
            get => _showDriverSuggestion;
            set => SetProperty(ref _showDriverSuggestion, value);
        }

        public ICommand ScanCommand { get; }
        public ICommand OpenDeviceManagerCommand { get; }
        public ICommand OpenWindowsUpdateCommand { get; }

        public DriverViewModel(IDriverService driverService)
        {
            _driverService = driverService ?? throw new ArgumentNullException(nameof(driverService));

            ScanCommand = new AsyncRelayCommand(ScanDriversAsync);
            OpenDeviceManagerCommand = new RelayCommand(OpenDeviceManager);
            OpenWindowsUpdateCommand = new RelayCommand(OpenWindowsUpdate);

            _ = ScanDriversAsync();
        }

        private async Task ScanDriversAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            StatusMessage = "Analizando controladores de hardware...";

            try
            {
                // Escaneo de controladores locales
                var installed = await _driverService.GetInstalledDriversAsync();
                Drivers = new ObservableCollection<DriverItem>(installed);
                
                TotalDriversCount = installed.Count;
                OutdatedDriversCount = installed.Count(d => d.IsOutdated);

                // Escaneo de actualizaciones COM de Windows Update
                StatusMessage = "Buscando actualizaciones de controladores en Windows Update...";
                var updates = await _driverService.GetAvailableDriverUpdatesAsync();
                AvailableUpdates = new ObservableCollection<DriverUpdateItem>(updates);
                AvailableUpdatesCount = updates.Count;

                if (OutdatedDriversCount > 0)
                {
                    ShowDriverSuggestion = true;
                    DriverSuggestionText = $"💡 Estabilidad Recomendada: Tienes {OutdatedDriversCount} controlador(es) antiguo(s) en el sistema. Recomendamos verificar actualizaciones.";
                }
                else
                {
                    ShowDriverSuggestion = false;
                    DriverSuggestionText = string.Empty;
                }

                StatusMessage = $"Análisis completado: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error al escanear controladores.";
                Log.Error(ex, "Error al escanear controladores en DriverViewModel.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilters()
        {
            if (Drivers == null) return;

            var filtered = Drivers.AsEnumerable();

            // Filtrado por categoría
            if (!string.IsNullOrEmpty(SelectedCategory) && !SelectedCategory.Equals("TODOS", StringComparison.OrdinalIgnoreCase))
            {
                if (SelectedCategory.Equals("DISPLAY", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = filtered.Where(d => d.DeviceClass.Equals("DISPLAY", StringComparison.OrdinalIgnoreCase));
                }
                else if (SelectedCategory.Equals("NET", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = filtered.Where(d => d.DeviceClass.Equals("NET", StringComparison.OrdinalIgnoreCase));
                }
                else if (SelectedCategory.Equals("MEDIA", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = filtered.Where(d => d.DeviceClass.Equals("MEDIA", StringComparison.OrdinalIgnoreCase));
                }
                else if (SelectedCategory.Equals("BLUETOOTH", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = filtered.Where(d => d.DeviceClass.Equals("BLUETOOTH", StringComparison.OrdinalIgnoreCase));
                }
                else if (SelectedCategory.Equals("OTROS", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = filtered.Where(d => 
                        !d.DeviceClass.Equals("DISPLAY", StringComparison.OrdinalIgnoreCase) &&
                        !d.DeviceClass.Equals("NET", StringComparison.OrdinalIgnoreCase) &&
                        !d.DeviceClass.Equals("MEDIA", StringComparison.OrdinalIgnoreCase) &&
                        !d.DeviceClass.Equals("BLUETOOTH", StringComparison.OrdinalIgnoreCase));
                }
            }

            // Filtrado por texto de búsqueda
            if (!string.IsNullOrEmpty(SearchText))
            {
                string query = SearchText.ToLower();
                filtered = filtered.Where(d => 
                    d.DeviceName.ToLower().Contains(query) || 
                    d.DriverProvider.ToLower().Contains(query) ||
                    d.DeviceClass.ToLower().Contains(query));
            }

            FilteredDrivers = new ObservableCollection<DriverItem>(filtered.ToList());
        }

        private void OpenDeviceManager()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "devmgmt.msc",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al abrir devmgmt.msc.");
            }
        }

        private void OpenWindowsUpdate()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:windowsupdate-action",
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                try
                {
                    // Fallback
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "ms-settings:windowsupdate",
                        UseShellExecute = true
                    });
                }
                catch (Exception iex)
                {
                    Log.Error(iex, "Error al abrir la configuración de Windows Update.");
                }
            }
        }
    }
}
