using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.ViewModels
{
    public class ServicesViewModel : ViewModelBase
    {
        private readonly IWindowsServicesService _servicesService;

        private List<ServiceItem> _services = new();
        private ServiceItem? _selectedService;
        private string _searchText = string.Empty;
        private bool _isLoading;
        private string _statusMessage = "Listo";

        public List<ServiceItem> FilteredServices
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    return _services;
                }
                return _services
                    .Where(s => s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                                s.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        public ServiceItem? SelectedService
        {
            get => _selectedService;
            set
            {
                if (SetProperty(ref _selectedService, value))
                {
                    OnPropertyChanged(nameof(IsServiceSelected));
                    OnPropertyChanged(nameof(CanStartSelected));
                    OnPropertyChanged(nameof(CanStopSelected));
                    OnPropertyChanged(nameof(CanModifySelected));
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    OnPropertyChanged(nameof(FilteredServices));
                }
            }
        }

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

        public bool IsServiceSelected => SelectedService != null;
        public bool CanStartSelected => SelectedService != null && !SelectedService.IsRunning && !SelectedService.IsCritical;
        public bool CanStopSelected => SelectedService != null && SelectedService.IsRunning && SelectedService.CanStop;
        public bool CanModifySelected => SelectedService != null && !SelectedService.IsCritical;

        public ICommand LoadServicesCommand { get; }
        public ICommand StartServiceCommand { get; }
        public ICommand StopServiceCommand { get; }
        public ICommand ChangeStartTypeCommand { get; }

        public ServicesViewModel(IWindowsServicesService servicesService)
        {
            _servicesService = servicesService ?? throw new ArgumentNullException(nameof(servicesService));

            LoadServicesCommand = new AsyncRelayCommand(LoadServicesAsync);
            StartServiceCommand = new AsyncRelayCommand(StartServiceAsync);
            StopServiceCommand = new AsyncRelayCommand(StopServiceAsync);
            ChangeStartTypeCommand = new AsyncRelayCommand<string>(ChangeStartTypeAsync);

            // Cargar servicios al iniciar
            _ = LoadServicesAsync();
        }

        private async Task LoadServicesAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            StatusMessage = "Cargando servicios de Windows...";
            SelectedService = null;

            try
            {
                var list = await _servicesService.GetServicesAsync(CancellationToken.None);
                _services = list;
                OnPropertyChanged(nameof(FilteredServices));
                StatusMessage = $"Se cargaron {_services.Count} servicios del sistema.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error al cargar los servicios.";
                Serilog.Log.Error(ex, "Error en ServicesViewModel.LoadServicesAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task StartServiceAsync()
        {
            if (SelectedService == null || SelectedService.IsRunning) return;

            IsLoading = true;
            StatusMessage = $"Iniciando servicio {SelectedService.DisplayName}...";

            try
            {
                bool success = await _servicesService.StartServiceAsync(SelectedService.Name, CancellationToken.None);
                if (success)
                {
                    SelectedService.IsRunning = true;
                    StatusMessage = $"Servicio {SelectedService.DisplayName} iniciado correctamente.";
                    // Actualizar estados de botones
                    OnPropertyChanged(nameof(CanStartSelected));
                    OnPropertyChanged(nameof(CanStopSelected));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al iniciar el servicio: {ex.Message}";
                MessageBox.Show($"No se pudo iniciar el servicio.\nDetalle: {ex.Message}\n\nNota: Es posible que necesite ejecutar WinCleaner como Administrador.", 
                                "Error de Permisos / Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task StopServiceAsync()
        {
            if (SelectedService == null || !SelectedService.IsRunning || !SelectedService.CanStop) return;

            IsLoading = true;
            StatusMessage = $"Deteniendo servicio {SelectedService.DisplayName}...";

            try
            {
                bool success = await _servicesService.StopServiceAsync(SelectedService.Name, CancellationToken.None);
                if (success)
                {
                    SelectedService.IsRunning = false;
                    StatusMessage = $"Servicio {SelectedService.DisplayName} detenido correctamente.";
                    // Actualizar estados de botones
                    OnPropertyChanged(nameof(CanStartSelected));
                    OnPropertyChanged(nameof(CanStopSelected));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al detener el servicio: {ex.Message}";
                MessageBox.Show($"No se pudo detener el servicio.\nDetalle: {ex.Message}\n\nNota: Es posible que necesite ejecutar WinCleaner como Administrador.", 
                                "Error de Permisos / Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ChangeStartTypeAsync(string? startType)
        {
            if (SelectedService == null || string.IsNullOrEmpty(startType) || SelectedService.IsCritical) return;

            IsLoading = true;
            StatusMessage = $"Cambiando tipo de inicio a {startType}...";

            try
            {
                bool success = await _servicesService.ChangeServiceStartTypeAsync(SelectedService.Name, startType, CancellationToken.None);
                if (success)
                {
                    SelectedService.StartType = startType;
                    StatusMessage = $"Tipo de inicio cambiado a {startType} para {SelectedService.DisplayName}.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cambiar tipo de inicio: {ex.Message}";
                MessageBox.Show($"No se pudo cambiar el tipo de inicio.\nDetalle: {ex.Message}\n\nNota: Modificar la configuración de servicios requiere ejecutar WinCleaner como Administrador.", 
                                "Error de Permisos", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
