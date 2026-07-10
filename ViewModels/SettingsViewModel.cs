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

        private string _maintenanceNextRunText = "Cargando...";

        private ObservableCollection<string> _excludedDirectories = null!;
        private ObservableCollection<string> _customScanDirectories = null!;

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

        public SettingsViewModel(IConfigurationService configurationService, IScheduledMaintenanceService maintenanceService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _maintenanceService = maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));

            // Vincular colecciones observables y responder a cambios de colección automáticamente
            InitializeCollections();

            AddExcludedDirectoryCommand = new RelayCommand(AddExcludedDirectory);
            RemoveExcludedDirectoryCommand = new RelayCommand<string>(RemoveExcludedDirectory);
            AddCustomDirectoryCommand = new RelayCommand(AddCustomDirectory);
            RemoveCustomDirectoryCommand = new RelayCommand<string>(RemoveCustomDirectory);
            SaveMaintenanceCommand = new RelayCommand(SaveMaintenanceSettings);

            RefreshMaintenanceStatus();
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
