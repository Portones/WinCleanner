using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IConfigurationService _configurationService;
        private readonly IScheduledMaintenanceService _maintenanceService;
        private readonly IStartupManagerService _startupManager;
        private readonly IWinCleanerUpdateService _winCleanerUpdateService;

        private string _maintenanceNextRunText = "Cargando...";

        private ObservableCollection<string> _excludedDirectories = null!;
        private ObservableCollection<string> _customScanDirectories = null!;

        // Propiedades de Auto-Actualización
        private bool _isCheckingUpdates;
        private bool _isDownloadingUpdate;
        private double _downloadProgress;
        private string _updateStatusText = "Sin comprobar";
        private string _latestVersionText = string.Empty;
        private string _releaseNotesText = string.Empty;
        private bool _isUpdateAvailable;
        private string _updateDownloadUrl = string.Empty;

        public bool AutoCheckUpdates
        {
            get => _configurationService.CurrentSettings.AutoCheckUpdates;
            set
            {
                if (_configurationService.CurrentSettings.AutoCheckUpdates != value)
                {
                    _configurationService.CurrentSettings.AutoCheckUpdates = value;
                    _configurationService.SaveSettings();
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCheckingUpdates
        {
            get => _isCheckingUpdates;
            set => SetProperty(ref _isCheckingUpdates, value);
        }

        public bool IsDownloadingUpdate
        {
            get => _isDownloadingUpdate;
            set => SetProperty(ref _isDownloadingUpdate, value);
        }

        public double DownloadProgress
        {
            get => _downloadProgress;
            set => SetProperty(ref _downloadProgress, value);
        }

        public string UpdateStatusText
        {
            get => _updateStatusText;
            set => SetProperty(ref _updateStatusText, value);
        }

        public string LatestVersionText
        {
            get => _latestVersionText;
            set => SetProperty(ref _latestVersionText, value);
        }

        public string ReleaseNotesText
        {
            get => _releaseNotesText;
            set => SetProperty(ref _releaseNotesText, value);
        }

        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            set => SetProperty(ref _isUpdateAvailable, value);
        }

        public string UpdateDownloadUrl
        {
            get => _updateDownloadUrl;
            set => SetProperty(ref _updateDownloadUrl, value);
        }

        public bool BypassRecycleBin
        {
            get => _configurationService.CurrentSettings.BypassRecycleBin;
            set
            {
                if (_configurationService.CurrentSettings.BypassRecycleBin != value)
                {
                    _configurationService.CurrentSettings.BypassRecycleBin = value;
                    _configurationService.SaveSettings();
                    OnPropertyChanged();
                }
            }
        }

        public bool StartWithWindows
        {
            get => _configurationService.CurrentSettings.StartWithWindows;
            set
            {
                if (_configurationService.CurrentSettings.StartWithWindows != value)
                {
                    _configurationService.CurrentSettings.StartWithWindows = value;
                    _configurationService.SaveSettings();
                    _startupManager.SetWindowsAutoStart(value, StartMinimized);
                    OnPropertyChanged();
                }
            }
        }

        public bool StartMinimized
        {
            get => _configurationService.CurrentSettings.StartMinimized;
            set
            {
                if (_configurationService.CurrentSettings.StartMinimized != value)
                {
                    _configurationService.CurrentSettings.StartMinimized = value;
                    _configurationService.SaveSettings();
                    if (StartWithWindows)
                    {
                        _startupManager.SetWindowsAutoStart(true, value);
                    }
                    OnPropertyChanged();
                }
            }
        }

        public bool MinimizeToTray
        {
            get => _configurationService.CurrentSettings.MinimizeToTray;
            set
            {
                if (_configurationService.CurrentSettings.MinimizeToTray != value)
                {
                    _configurationService.CurrentSettings.MinimizeToTray = value;
                    _configurationService.SaveSettings();
                    OnPropertyChanged();
                }
            }
        }

        public bool EnableBackgroundMonitoring
        {
            get => _configurationService.CurrentSettings.EnableBackgroundMonitoring;
            set
            {
                if (_configurationService.CurrentSettings.EnableBackgroundMonitoring != value)
                {
                    _configurationService.CurrentSettings.EnableBackgroundMonitoring = value;
                    _configurationService.SaveSettings();
                    OnPropertyChanged();
                }
            }
        }

        public bool NotifyHighRam
        {
            get => _configurationService.CurrentSettings.NotifyHighRam;
            set
            {
                if (_configurationService.CurrentSettings.NotifyHighRam != value)
                {
                    _configurationService.CurrentSettings.NotifyHighRam = value;
                    _configurationService.SaveSettings();
                    OnPropertyChanged();
                }
            }
        }

        public bool NotifyLowDiskSpace
        {
            get => _configurationService.CurrentSettings.NotifyLowDiskSpace;
            set
            {
                if (_configurationService.CurrentSettings.NotifyLowDiskSpace != value)
                {
                    _configurationService.CurrentSettings.NotifyLowDiskSpace = value;
                    _configurationService.SaveSettings();
                    OnPropertyChanged();
                }
            }
        }

        public bool NotifyHighTemp
        {
            get => _configurationService.CurrentSettings.NotifyHighTemp;
            set
            {
                if (_configurationService.CurrentSettings.NotifyHighTemp != value)
                {
                    _configurationService.CurrentSettings.NotifyHighTemp = value;
                    _configurationService.SaveSettings();
                    OnPropertyChanged();
                }
            }
        }

        public long MinLargeFileSizeMb
        {
            get => _configurationService.CurrentSettings.MinLargeFileSizeMb;
            set
            {
                if (_configurationService.CurrentSettings.MinLargeFileSizeMb != value)
                {
                    _configurationService.CurrentSettings.MinLargeFileSizeMb = value;
                    _configurationService.SaveSettings();
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> ExcludedDirectories
        {
            get => _excludedDirectories;
            set => SetProperty(ref _excludedDirectories, value);
        }

        public ObservableCollection<string> CustomScanDirectories
        {
            get => _customScanDirectories;
            set => SetProperty(ref _customScanDirectories, value);
        }

        public bool MaintenanceTaskEnabled
        {
            get => _configurationService.CurrentSettings.MaintenanceTaskEnabled;
            set
            {
                if (_configurationService.CurrentSettings.MaintenanceTaskEnabled != value)
                {
                    _configurationService.CurrentSettings.MaintenanceTaskEnabled = value;
                    _configurationService.SaveSettings();
                    OnPropertyChanged();
                    UpdateMaintenanceTaskState();
                }
            }
        }

        public string MaintenanceFrequency
        {
            get => _configurationService.CurrentSettings.MaintenanceFrequency;
            set
            {
                if (_configurationService.CurrentSettings.MaintenanceFrequency != value)
                {
                    _configurationService.CurrentSettings.MaintenanceFrequency = value;
                    _configurationService.SaveSettings();
                    OnPropertyChanged();
                }
            }
        }

        public string MaintenanceDay
        {
            get => _configurationService.CurrentSettings.MaintenanceDay;
            set
            {
                if (_configurationService.CurrentSettings.MaintenanceDay != value)
                {
                    _configurationService.CurrentSettings.MaintenanceDay = value;
                    _configurationService.SaveSettings();
                    OnPropertyChanged();
                }
            }
        }

        public string MaintenanceTime
        {
            get => _configurationService.CurrentSettings.MaintenanceTime;
            set
            {
                if (_configurationService.CurrentSettings.MaintenanceTime != value)
                {
                    _configurationService.CurrentSettings.MaintenanceTime = value;
                    _configurationService.SaveSettings();
                    OnPropertyChanged();
                }
            }
        }

        public string MaintenanceNextRunText
        {
            get => _maintenanceNextRunText;
            set => SetProperty(ref _maintenanceNextRunText, value);
        }

        public ICommand AddExcludedDirectoryCommand { get; }
        public ICommand RemoveExcludedDirectoryCommand { get; }
        public ICommand AddCustomDirectoryCommand { get; }
        public ICommand RemoveCustomDirectoryCommand { get; }
        public ICommand SaveMaintenanceCommand { get; }
        public ICommand CheckForUpdatesCommand { get; }
        public ICommand InstallUpdateCommand { get; }

        public SettingsViewModel(
            IConfigurationService configurationService, 
            IScheduledMaintenanceService maintenanceService,
            IStartupManagerService startupManager,
            IWinCleanerUpdateService winCleanerUpdateService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _maintenanceService = maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));
            _startupManager = startupManager ?? throw new ArgumentNullException(nameof(startupManager));
            _winCleanerUpdateService = winCleanerUpdateService ?? throw new ArgumentNullException(nameof(winCleanerUpdateService));

            // Vincular colecciones observables y responder a cambios de colección automáticamente
            InitializeCollections();

            AddExcludedDirectoryCommand = new RelayCommand(AddExcludedDirectory);
            RemoveExcludedDirectoryCommand = new RelayCommand<string>(RemoveExcludedDirectory);
            AddCustomDirectoryCommand = new RelayCommand(AddCustomDirectory);
            RemoveCustomDirectoryCommand = new RelayCommand<string>(RemoveCustomDirectory);
            SaveMaintenanceCommand = new RelayCommand(SaveMaintenanceSettings);
            CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync);
            InstallUpdateCommand = new AsyncRelayCommand(InstallUpdateAsync);

            RefreshMaintenanceStatus();

            _winCleanerUpdateService.UpdateChecked += OnUpdateChecked;

            // Cargar información de actualización previamente buscada (ej. comprobación automática al arrancar)
            if (_winCleanerUpdateService.LastUpdateInfo != null)
            {
                var result = _winCleanerUpdateService.LastUpdateInfo;
                LatestVersionText = result.LatestVersion;
                ReleaseNotesText = result.ReleaseNotes;
                UpdateDownloadUrl = result.DownloadUrl;
                IsUpdateAvailable = result.IsUpdateAvailable;

                if (result.IsUpdateAvailable)
                {
                    UpdateStatusText = $"¡Nueva versión v{result.LatestVersion} disponible!";
                }
                else if (!string.IsNullOrEmpty(result.LatestVersion))
                {
                    UpdateStatusText = $"WinCleaner está actualizado (v{result.CurrentVersion}).";
                }
            }
        }

        private void OnUpdateChecked(WinCleaner.Services.Interfaces.WinCleanerUpdateInfo result)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                LatestVersionText = result.LatestVersion;
                ReleaseNotesText = result.ReleaseNotes;
                UpdateDownloadUrl = result.DownloadUrl;
                IsUpdateAvailable = result.IsUpdateAvailable;

                if (result.IsUpdateAvailable)
                {
                    UpdateStatusText = $"¡Nueva versión v{result.LatestVersion} disponible!";
                }
                else if (!string.IsNullOrEmpty(result.LatestVersion))
                {
                    UpdateStatusText = $"WinCleaner está actualizado (v{result.CurrentVersion}).";
                }
            });
        }

        public async System.Threading.Tasks.Task CheckForUpdatesAsync()
        {
            IsCheckingUpdates = true;
            UpdateStatusText = "Buscando actualizaciones en GitHub...";
            try
            {
                var result = await _winCleanerUpdateService.CheckForUpdatesAsync();
                LatestVersionText = result.LatestVersion;
                ReleaseNotesText = result.ReleaseNotes;
                UpdateDownloadUrl = result.DownloadUrl;
                IsUpdateAvailable = result.IsUpdateAvailable;

                if (result.IsUpdateAvailable)
                {
                    UpdateStatusText = $"¡Nueva versión v{result.LatestVersion} disponible!";
                }
                else if (!string.IsNullOrEmpty(result.LatestVersion))
                {
                    UpdateStatusText = $"WinCleaner está actualizado (v{result.CurrentVersion}).";
                }
                else
                {
                    UpdateStatusText = "No se pudo consultar el servidor de actualizaciones.";
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText = $"Error al buscar actualizaciones: {ex.Message}";
            }
            finally
            {
                IsCheckingUpdates = false;
            }
        }

        public async System.Threading.Tasks.Task InstallUpdateAsync()
        {
            if (string.IsNullOrEmpty(UpdateDownloadUrl))
            {
                UpdateStatusText = "Error: No se pudo obtener la URL de descarga para la actualización.";
                return;
            }

            IsDownloadingUpdate = true;
            DownloadProgress = 0;
            UpdateStatusText = "Iniciando descarga de la actualización...";
            try
            {
                var progress = new Progress<double>(p =>
                {
                    DownloadProgress = p;
                    UpdateStatusText = $"Descargando actualización... {p:0.0}%";
                });
                bool success = await _winCleanerUpdateService.DownloadAndInstallUpdateAsync(UpdateDownloadUrl, progress);
                
                if (!success)
                {
                    UpdateStatusText = "Ocurrió un error al descargar o iniciar la instalación.";
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText = $"Error al instalar actualización: {ex.Message}";
            }
            finally
            {
                IsDownloadingUpdate = false;
            }
        }

        private void InitializeCollections()
        {
            _excludedDirectories = new ObservableCollection<string>(_configurationService.CurrentSettings.ExcludedDirectories);
            _excludedDirectories.CollectionChanged += (s, e) =>
            {
                _configurationService.CurrentSettings.ExcludedDirectories = _excludedDirectories.ToList();
                _configurationService.SaveSettings();
            };

            _customScanDirectories = new ObservableCollection<string>(_configurationService.CurrentSettings.CustomScanDirectories);
            _customScanDirectories.CollectionChanged += (s, e) =>
            {
                _configurationService.CurrentSettings.CustomScanDirectories = _customScanDirectories.ToList();
                _configurationService.SaveSettings();
            };
        }

        private void AddExcludedDirectory()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Seleccione una Carpeta para Excluir de la Limpieza",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FolderName;
                if (!string.IsNullOrEmpty(path) && !_excludedDirectories.Contains(path))
                {
                    _excludedDirectories.Add(path);
                }
            }
        }

        private void RemoveExcludedDirectory(string? path)
        {
            if (!string.IsNullOrEmpty(path) && _excludedDirectories.Contains(path))
            {
                _excludedDirectories.Remove(path);
            }
        }

        private void AddCustomDirectory()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Seleccione una Carpeta para Incluir en Análisis Personalizados",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FolderName;
                if (!string.IsNullOrEmpty(path) && !_customScanDirectories.Contains(path))
                {
                    _customScanDirectories.Add(path);
                }
            }
        }

        private void RemoveCustomDirectory(string? path)
        {
            if (!string.IsNullOrEmpty(path) && _customScanDirectories.Contains(path))
            {
                _customScanDirectories.Remove(path);
            }
        }

        private void RefreshMaintenanceStatus()
        {
            bool realEnabled = _maintenanceService.IsTaskEnabled();
            if (realEnabled != _configurationService.CurrentSettings.MaintenanceTaskEnabled)
            {
                _configurationService.CurrentSettings.MaintenanceTaskEnabled = realEnabled;
                _configurationService.SaveSettings();
                OnPropertyChanged(nameof(MaintenanceTaskEnabled));
            }

            MaintenanceNextRunText = realEnabled ? _maintenanceService.GetTaskNextRunTime() : "Desactivada";
        }

        private void UpdateMaintenanceTaskState()
        {
            try
            {
                if (MaintenanceTaskEnabled)
                {
                    _maintenanceService.EnableMaintenanceTask(
                        MaintenanceFrequency,
                        MaintenanceDay,
                        MaintenanceTime
                    );
                    System.Windows.MessageBox.Show("Tarea de mantenimiento programada correctamente en el sistema.", 
                                                    "Mantenimiento Programado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    _maintenanceService.DisableMaintenanceTask();
                    System.Windows.MessageBox.Show("Tarea de mantenimiento eliminada del sistema.", 
                                                    "Mantenimiento Programado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                RefreshMaintenanceStatus();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al conmutar el estado de la tarea:\n{ex.Message}", 
                                                "Error de Programación", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                _configurationService.CurrentSettings.MaintenanceTaskEnabled = !MaintenanceTaskEnabled;
                _configurationService.SaveSettings();
                OnPropertyChanged(nameof(MaintenanceTaskEnabled));
            }
        }

        private void SaveMaintenanceSettings()
        {
            if (!MaintenanceTaskEnabled)
            {
                System.Windows.MessageBox.Show("Active la casilla de habilitación para programar la tarea.", 
                                                "Mantenimiento Programado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                _maintenanceService.EnableMaintenanceTask(
                    MaintenanceFrequency,
                    MaintenanceDay,
                    MaintenanceTime
                );
                System.Windows.MessageBox.Show("Configuración de mantenimiento automático guardada y aplicada en Windows.", 
                                                "Configuración Guardada", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                RefreshMaintenanceStatus();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al guardar la tarea programada:\n{ex.Message}", 
                                                "Error de Configuración", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
