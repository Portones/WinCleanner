using System;
using System.Collections.Generic;
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
    public class UninstallerViewModel : ViewModelBase
    {
        private readonly IAppUninstallerService _uninstallerService;

        private List<InstalledApp> _apps = new();
        private InstalledApp? _selectedApp;
        private string _searchText = string.Empty;
        private bool _showStoreApps = true;
        private bool _showDesktopApps = true;
        private bool _isLoading;
        private string _statusMessage = "Listo";
        private List<ResidualItem> _residuals = new();
        private bool _showResidualsCard;

        public List<InstalledApp> FilteredApps
        {
            get
            {
                var query = _apps.AsQueryable();

                if (!ShowStoreApps)
                {
                    query = query.Where(a => !a.IsUwp);
                }
                if (!ShowDesktopApps)
                {
                    query = query.Where(a => a.IsUwp);
                }

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string search = SearchText.Trim();
                    query = query.Where(a => a.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                                             a.Publisher.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                return query.ToList();
            }
        }

        public InstalledApp? SelectedApp
        {
            get => _selectedApp;
            set
            {
                if (SetProperty(ref _selectedApp, value))
                {
                    OnPropertyChanged(nameof(IsAppSelected));
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
                    OnPropertyChanged(nameof(FilteredApps));
                }
            }
        }

        public bool ShowStoreApps
        {
            get => _showStoreApps;
            set
            {
                if (SetProperty(ref _showStoreApps, value))
                {
                    OnPropertyChanged(nameof(FilteredApps));
                }
            }
        }

        public bool ShowDesktopApps
        {
            get => _showDesktopApps;
            set
            {
                if (SetProperty(ref _showDesktopApps, value))
                {
                    OnPropertyChanged(nameof(FilteredApps));
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

        public List<ResidualItem> Residuals
        {
            get => _residuals;
            set => SetProperty(ref _residuals, value);
        }

        public bool ShowResidualsCard
        {
            get => _showResidualsCard;
            set => SetProperty(ref _showResidualsCard, value);
        }

        public bool IsAppSelected => SelectedApp != null;

        public ICommand LoadAppsCommand { get; }
        public ICommand UninstallCommand { get; }
        public ICommand CleanResidualsCommand { get; }
        public ICommand CloseResidualsCardCommand { get; }

        public UninstallerViewModel(IAppUninstallerService uninstallerService)
        {
            _uninstallerService = uninstallerService ?? throw new ArgumentNullException(nameof(uninstallerService));

            LoadAppsCommand = new AsyncRelayCommand(LoadAppsAsync);
            UninstallCommand = new AsyncRelayCommand<InstalledApp>(UninstallAsync);
            CleanResidualsCommand = new AsyncRelayCommand(CleanResidualsAsync);
            CloseResidualsCardCommand = new RelayCommand(() => ShowResidualsCard = false);

            _ = LoadAppsAsync();
        }

        private async Task LoadAppsAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            StatusMessage = "Escaneando aplicaciones instaladas...";
            SelectedApp = null;
            ShowResidualsCard = false;

            try
            {
                var list = await _uninstallerService.GetInstalledAppsAsync(CancellationToken.None);
                _apps = list;
                OnPropertyChanged(nameof(FilteredApps));
                StatusMessage = $"Se encontraron {_apps.Count} aplicaciones en el equipo.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error al escanear aplicaciones.";
                Serilog.Log.Error(ex, "Error en UninstallerViewModel.LoadAppsAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UninstallAsync(InstalledApp? app)
        {
            // Acepta la app pasada como parámetro; si es null usa SelectedApp como fallback
            var appToUninstall = app ?? SelectedApp;
            if (appToUninstall == null) return;

            var result = MessageBox.Show(
                $"¿Desinstalar '{appToUninstall.DisplayName}'?\n\nEsta acción eliminará el programa de tu equipo.",
                "Confirmar Desinstalación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;
            StatusMessage = $"Desinstalando {appToUninstall.DisplayName}...";

            try
            {
                bool success = await _uninstallerService.UninstallAppAsync(appToUninstall, CancellationToken.None);
                if (success)
                {
                    StatusMessage = $"Desinstalador completado para {appToUninstall.DisplayName}. Escaneando residuos...";
                    var leftovers = await _uninstallerService.ScanResidualsAsync(appToUninstall, CancellationToken.None);
                    await LoadAppsAsync();

                    if (leftovers != null && leftovers.Count > 0)
                    {
                        Residuals = leftovers;
                        ShowResidualsCard = true;
                        StatusMessage = $"Se encontraron {leftovers.Count} residuos de {appToUninstall.DisplayName}.";
                    }
                    else
                    {
                        MessageBox.Show(
                            $"'{appToUninstall.DisplayName}' se desinstaló correctamente.\nNo se detectaron archivos ni registros residuales.",
                            "Desinstalación Completada", MessageBoxButton.OK, MessageBoxImage.Information);
                        StatusMessage = "Desinstalación completada. No se encontraron residuos.";
                    }
                }
                else
                {
                    StatusMessage = $"La desinstalación de {appToUninstall.DisplayName} fue cancelada o falló.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al desinstalar: {ex.Message}";
                var forceOption = MessageBox.Show(
                    $"El desinstalador oficial no se pudo iniciar o falló.\n\nDetalle: {ex.Message}\n\n¿Desea forzar la eliminación de la entrada en el registro de Windows y buscar posibles archivos o carpetas huérfanas de '{appToUninstall.DisplayName}'?",
                    "Fallo de Desinstalador - Forzar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (forceOption == MessageBoxResult.Yes)
                {
                    StatusMessage = $"Forzando eliminación de {appToUninstall.DisplayName}...";
                    bool removed = await _uninstallerService.ForceRemoveAppEntryAsync(appToUninstall);
                    if (removed)
                    {
                        StatusMessage = "Entrada de registro eliminada. Buscando residuos huérfanos...";
                        var leftovers = await _uninstallerService.ScanResidualsAsync(appToUninstall, CancellationToken.None);
                        await LoadAppsAsync();

                        if (leftovers != null && leftovers.Count > 0)
                        {
                            Residuals = leftovers;
                            ShowResidualsCard = true;
                            StatusMessage = $"Registro quitado. Se encontraron {leftovers.Count} residuos huérfanos.";
                        }
                        else
                        {
                            MessageBox.Show(
                                $"Se eliminó la entrada de '{appToUninstall.DisplayName}' del registro de Windows.\nNo se detectaron más archivos residuales.",
                                "Eliminación Completada", MessageBoxButton.OK, MessageBoxImage.Information);
                            StatusMessage = "Eliminación forzada completada sin residuos.";
                        }
                    }
                    else
                    {
                        MessageBox.Show("No se pudo eliminar la entrada del registro. Es posible que la entrada ya no exista o requiera permisos elevados.",
                                        "Error al forzar eliminación", MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusMessage = "Fallo al forzar la eliminación.";
                    }
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CleanResidualsAsync()
        {
            if (Residuals == null || Residuals.Count == 0) return;

            var itemsToClean = Residuals.Where(r => r.IsSelected).ToList();
            if (itemsToClean.Count == 0)
            {
                MessageBox.Show("Seleccione al menos un residuo para limpiar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsLoading = true;
            StatusMessage = "Limpiando archivos y registros residuales...";

            try
            {
                bool success = await _uninstallerService.CleanResidualsAsync(itemsToClean, CancellationToken.None);
                ShowResidualsCard = false;
                Residuals = new List<ResidualItem>();

                if (success)
                {
                    StatusMessage = "Limpieza de residuos completada con éxito.";
                    MessageBox.Show("Todos los elementos residuales seleccionados fueron eliminados del equipo.", 
                                    "Limpieza Completada", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = "Limpieza de residuos finalizada con advertencias.";
                    MessageBox.Show("Algunos residuos no se pudieron eliminar (pueden estar bloqueados por el sistema).", 
                                    "Limpieza Finalizada", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al limpiar residuos: {ex.Message}";
                MessageBox.Show($"Error al intentar limpiar los residuos.\nDetalle: {ex.Message}", 
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
