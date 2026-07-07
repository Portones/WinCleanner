using System;
using System.Collections.Generic;
using System.IO;
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
    public class DuplicateFilesViewModel : ViewModelBase
    {
        private readonly IDuplicateFinderService _duplicateFinder;
        private readonly IConfigurationService _configurationService;
        private CancellationTokenSource? _scanCts;

        private List<DuplicateGroup> _duplicateGroups = new();
        private bool _isScanning;
        private bool _isCleaning;
        private double _progress;
        private string _statusMessage = "Listo para buscar archivos duplicados.";
        private long _totalPotentialSpace;
        private string _totalPotentialSpaceText = "0 Bytes";
        private long _selectedSpace;
        private string _selectedSpaceText = "0 Bytes";

        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                if (SetProperty(ref _isScanning, value))
                {
                    OnPropertyChanged(nameof(CanScan));
                    OnPropertyChanged(nameof(CanClean));
                }
            }
        }

        public bool IsCleaning
        {
            get => _isCleaning;
            set
            {
                if (SetProperty(ref _isCleaning, value))
                {
                    OnPropertyChanged(nameof(CanScan));
                    OnPropertyChanged(nameof(CanClean));
                }
            }
        }

        public bool CanScan => !IsScanning && !IsCleaning;
        public bool CanClean => !IsScanning && !IsCleaning && _duplicateGroups.Count > 0;

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public List<DuplicateGroup> DuplicateGroups
        {
            get => _duplicateGroups;
            set => SetProperty(ref _duplicateGroups, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string TotalPotentialSpaceText
        {
            get => _totalPotentialSpaceText;
            set => SetProperty(ref _totalPotentialSpaceText, value);
        }

        public string SelectedSpaceText
        {
            get => _selectedSpaceText;
            set => SetProperty(ref _selectedSpaceText, value);
        }

        public ICommand ScanCommand { get; }
        public ICommand CancelScanCommand { get; }
        public ICommand CleanCommand { get; }

        public DuplicateFilesViewModel(IDuplicateFinderService duplicateFinder, IConfigurationService configurationService)
        {
            _duplicateFinder = duplicateFinder ?? throw new ArgumentNullException(nameof(duplicateFinder));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));

            ScanCommand = new AsyncRelayCommand(ScanAsync);
            CancelScanCommand = new RelayCommand(CancelScan);
            CleanCommand = new AsyncRelayCommand(CleanAsync);
        }

        private async Task ScanAsync()
        {
            if (IsScanning || IsCleaning) return;

            IsScanning = true;
            Progress = 0;
            StatusMessage = "Escaneando directorios de usuario en busca de archivos duplicados...";
            _scanCts = new CancellationTokenSource();

            DuplicateGroups = new List<DuplicateGroup>();
            _totalPotentialSpace = 0;
            TotalPotentialSpaceText = "0 Bytes";
            _selectedSpace = 0;
            SelectedSpaceText = "0 Bytes";
            OnPropertyChanged(nameof(CanClean));

            try
            {
                var scanPaths = GetScanPaths();
                var progressIndicator = new Progress<double>(val => Progress = val);

                var groups = await _duplicateFinder.FindDuplicatesAsync(scanPaths, progressIndicator, _scanCts.Token);
                
                DuplicateGroups = groups;
                
                // Calcular espacio potencial (suma de todos los archivos menos una copia que se conserva)
                _totalPotentialSpace = 0;
                foreach (var group in DuplicateGroups)
                {
                    // Si hay N archivos duplicados, se pueden borrar N-1 archivos
                    long groupSavable = group.Size * (group.Files.Count - 1);
                    _totalPotentialSpace += groupSavable;

                    // Enlazar evento property changed
                    foreach (var file in group.Files)
                    {
                        file.PropertyChanged += File_PropertyChanged;
                    }
                }

                TotalPotentialSpaceText = CleanableItem.FormatSize(_totalPotentialSpace);
                UpdateSelectedSpace();
                OnPropertyChanged(nameof(CanClean));

                StatusMessage = $"Análisis completado. Encontrados {DuplicateGroups.Count} grupos de duplicados ({CleanableItem.FormatSize(_totalPotentialSpace)} de espacio redundante).";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Análisis cancelado por el usuario.";
                Progress = 0;
            }
            catch (Exception ex)
            {
                StatusMessage = "Ocurrió un error durante el escaneo de duplicados.";
                Log.Error(ex, "Error al buscar archivos duplicados en DuplicateFilesViewModel.");
            }
            finally
            {
                IsScanning = false;
                _scanCts = null;
            }
        }

        private void CancelScan()
        {
            _scanCts?.Cancel();
        }

        private async Task CleanAsync()
        {
            if (IsScanning || IsCleaning) return;

            var filesToClean = _duplicateGroups.SelectMany(g => g.Files).Where(f => f.IsSelected).ToList();
            if (filesToClean.Count == 0)
            {
                MessageBox.Show("Por favor, seleccione las copias redundantes que desea eliminar.", "Ningún archivo seleccionado", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Advertencia de seguridad: evitar borrar todas las copias de un grupo
            foreach (var group in _duplicateGroups)
            {
                var selectedInGroup = group.Files.Count(f => f.IsSelected);
                if (selectedInGroup == group.Files.Count)
                {
                    var confirm = MessageBox.Show(
                        $"¡ATENCIÓN! En el grupo '{group.Files[0].Name}' ha seleccionado TODAS las copias disponibles para eliminar. Si procede, el archivo desaparecerá de su ordenador para siempre.\n\n¿Desea continuar?",
                        "Advertencia de Pérdida de Archivo",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    
                    if (confirm != MessageBoxResult.Yes) return;
                }
            }

            bool permanent = _configurationService.CurrentSettings.BypassRecycleBin;

            if (permanent)
            {
                var confirm1 = MessageBox.Show(
                    "¿Desea eliminar PERMANENTEMENTE los archivos duplicados seleccionados?\nEsta acción no se puede deshacer.",
                    "Confirmación de Eliminación Permanente (Paso 1 de 2)",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm1 != MessageBoxResult.Yes) return;

                var confirm2 = MessageBox.Show(
                    "¿Confirmar borrado definitivo final?",
                    "Confirmación Final (Paso 2 de 2)",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm2 != MessageBoxResult.Yes) return;
            }

            IsCleaning = true;
            Progress = 0;
            StatusMessage = "Eliminando archivos duplicados seleccionados...";

            try
            {
                var progressIndicator = new Progress<double>(val => Progress = val);
                var token = CancellationToken.None;

                int cleanedCount = await _duplicateFinder.CleanDuplicatesAsync(filesToClean, permanent, progressIndicator, token);

                // Remover archivos limpiados de la vista
                foreach (var group in _duplicateGroups.ToList())
                {
                    foreach (var file in filesToClean)
                    {
                        if (group.Files.Contains(file))
                        {
                            file.PropertyChanged -= File_PropertyChanged;
                            group.Files.Remove(file);
                        }
                    }
                    
                    // Si el grupo se queda con 1 o 0 archivos, ya no es un duplicado, se remueve el grupo
                    if (group.Files.Count <= 1)
                    {
                        foreach (var remaining in group.Files)
                        {
                            remaining.PropertyChanged -= File_PropertyChanged;
                        }
                        _duplicateGroups.Remove(group);
                    }
                }

                DuplicateGroups = _duplicateGroups.ToList();

                _totalPotentialSpace = 0;
                foreach (var group in DuplicateGroups)
                {
                    long groupSavable = group.Size * (group.Files.Count - 1);
                    _totalPotentialSpace += groupSavable;
                }

                TotalPotentialSpaceText = CleanableItem.FormatSize(_totalPotentialSpace);
                UpdateSelectedSpace();
                OnPropertyChanged(nameof(CanClean));

                StatusMessage = $"Limpieza de duplicados completada. Se eliminaron {cleanedCount} copias de forma segura.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error al eliminar los archivos duplicados.";
                Log.Error(ex, "Error al eliminar duplicados en DuplicateFilesViewModel.");
            }
            finally
            {
                IsCleaning = false;
            }
        }

        private void File_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DuplicateFile.IsSelected))
            {
                UpdateSelectedSpace();
            }
        }

        private void UpdateSelectedSpace()
        {
            _selectedSpace = _duplicateGroups.SelectMany(g => g.Files).Where(f => f.IsSelected).Sum(f => f.Size);
            SelectedSpaceText = CleanableItem.FormatSize(_selectedSpace);
        }

        private List<string> GetScanPaths()
        {
            var list = new List<string>();
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var folders = new[] { "Downloads", "Documents", "Desktop" };
            foreach (var folder in folders)
            {
                var fullPath = Path.Combine(userProfile, folder);
                if (Directory.Exists(fullPath)) list.Add(fullPath);
            }

            foreach (var dir in _configurationService.CurrentSettings.CustomScanDirectories)
            {
                if (Directory.Exists(dir) && !list.Contains(dir))
                {
                    list.Add(dir);
                }
            }

            return list;
        }
    }
}
