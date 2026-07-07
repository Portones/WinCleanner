using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.ViewModels
{
    public class UpdaterViewModel : ViewModelBase
    {
        private readonly IAppUpdaterService _updaterService;

        private List<AppUpdateItem> _updates = new();
        private List<AppUpdateItem> _filteredUpdates = new();
        private string _searchText = string.Empty;
        private bool _isLoading;
        private string _statusMessage = "Listo. Presione 'Buscar Actualizaciones' para comenzar.";
        private double _progressValue;
        private bool _isUpgrading;
        private bool _isWingetAvailable = true;

        public List<AppUpdateItem> FilteredUpdates
        {
            get => _filteredUpdates;
            set => SetProperty(ref _filteredUpdates, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsUpgrading
        {
            get => _isUpgrading;
            set => SetProperty(ref _isUpgrading, value);
        }

        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsWingetAvailable
        {
            get => _isWingetAvailable;
            set => SetProperty(ref _isWingetAvailable, value);
        }

        public ICommand LoadUpdatesCommand { get; }
        public ICommand UpgradeSelectedCommand { get; }
        public ICommand ToggleAllSelectionCommand { get; }

        public UpdaterViewModel(IAppUpdaterService updaterService)
        {
            _updaterService = updaterService ?? throw new ArgumentNullException(nameof(updaterService));

            LoadUpdatesCommand = new AsyncRelayCommand(LoadUpdatesAsync);
            UpgradeSelectedCommand = new AsyncRelayCommand(UpgradeSelectedAsync);
            ToggleAllSelectionCommand = new RelayCommand<string>(ToggleAllSelection);

            // Verificar disponibilidad de Winget
            IsWingetAvailable = _updaterService.IsWingetInstalled();
            if (!IsWingetAvailable)
            {
                StatusMessage = "El Administrador de Paquetes de Windows (Winget) no está instalado en este equipo.";
            }

            // Cargar actualizaciones automáticamente al iniciar la pestaña
            _ = LoadUpdatesAsync();
        }

        private async Task LoadUpdatesAsync()
        {
            if (!IsWingetAvailable)
            {
                StatusMessage = "Winget no está disponible en este sistema. Instálelo desde Microsoft Store.";
                return;
            }

            if (IsLoading || IsUpgrading) return;

            IsLoading = true;
            StatusMessage = "Buscando actualizaciones disponibles en el sistema (winget)...";
            FilteredUpdates = new List<AppUpdateItem>();
            _updates.Clear();

            try
            {
                var list = await _updaterService.GetAvailableUpdatesAsync(CancellationToken.None);
                _updates = list;
                ApplyFilter();

                if (_updates.Count == 0)
                {
                    StatusMessage = "¡Enhorabuena! El sistema está al día. No hay actualizaciones disponibles.";
                }
                else
                {
                    StatusMessage = $"Se detectaron {_updates.Count} aplicaciones desactualizadas.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al buscar actualizaciones: {ex.Message}";
                Log.Error(ex, "Error en UpdaterViewModel.LoadUpdatesAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UpgradeSelectedAsync()
        {
            var selected = FilteredUpdates.Where(x => x.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Seleccione al menos una aplicación para actualizar.", "Sin selección", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show($"¿Desea descargar e instalar las {selected.Count} actualizaciones seleccionadas?\n\nLas instalaciones se realizarán en segundo plano de forma silenciosa.", 
                                          "Confirmar Actualizaciones", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            IsUpgrading = true;
            ProgressValue = 0;
            double step = 100.0 / selected.Count;

            try
            {
                for (int i = 0; i < selected.Count; i++)
                {
                    var app = selected[i];
                    StatusMessage = $"[{i + 1}/{selected.Count}] Actualizando {app.Name}...";

                    bool ok = await _updaterService.UpgradeAppAsync(app, CancellationToken.None);
                    ProgressValue += step;

                    if (!ok)
                    {
                        Log.Warning("No se pudo completar la actualización de: {AppName}", app.Name);
                    }
                }

                StatusMessage = "Proceso de actualización completado.";
                MessageBox.Show("Las actualizaciones seleccionadas han sido procesadas.\nLa lista se refrescará automáticamente.", 
                                "Actualización Completada", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refrescar listado
                IsUpgrading = false;
                await LoadUpdatesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error durante el proceso: {ex.Message}";
                Log.Error(ex, "Error en UpdaterViewModel.UpgradeSelectedAsync");
            }
            finally
            {
                IsUpgrading = false;
                ProgressValue = 0;
            }
        }

        private void ToggleAllSelection(string? selectAllStr)
        {
            bool selectAll = selectAllStr?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? true;
            foreach (var item in FilteredUpdates)
            {
                item.IsSelected = selectAll;
            }
            // Forzar refresco visual notificando el cambio de propiedad
            var temp = FilteredUpdates;
            FilteredUpdates = null!;
            FilteredUpdates = temp;
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredUpdates = new List<AppUpdateItem>(_updates);
            }
            else
            {
                FilteredUpdates = _updates
                    .Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                x.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }
    }
}
