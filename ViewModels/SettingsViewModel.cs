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

        public ICommand AddExcludedDirectoryCommand { get; }
        public ICommand RemoveExcludedDirectoryCommand { get; }
        public ICommand AddCustomDirectoryCommand { get; }
        public ICommand RemoveCustomDirectoryCommand { get; }

        public SettingsViewModel(IConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));

            // Vincular colecciones observables y responder a cambios de colección automáticamente
            InitializeCollections();

            AddExcludedDirectoryCommand = new RelayCommand(AddExcludedDirectory);
            RemoveExcludedDirectoryCommand = new RelayCommand<string>(RemoveExcludedDirectory);
            AddCustomDirectoryCommand = new RelayCommand(AddCustomDirectory);
            RemoveCustomDirectoryCommand = new RelayCommand<string>(RemoveCustomDirectory);
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
    }
}
