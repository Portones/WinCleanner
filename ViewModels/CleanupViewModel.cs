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
    public class CleanupViewModel : ViewModelBase
    {
        private readonly ICleanupManagerService _cleanupManager;
        private readonly IConfigurationService _configurationService;
        private CancellationTokenSource? _scanCts;

        private List<CleanableItem> _allItems = new();
        private List<CleanableItem> _displayItems = new();
        private bool _isScanning;
        private bool _isCleaning;
        private double _progress;
        private string _searchText = string.Empty;
        private string _selectedCategory = "Todos";
        private List<string> _categories = new() { "Todos" };
        
        private long _totalFoundSize;
        private string _totalFoundSizeText = "0 Bytes";
        private long _selectedSize;
        private string _selectedSizeText = "0 Bytes";
        private string _statusMessage = "Listo para iniciar escaneo.";

        private string _currentSortOption = "Tamaño"; // Tamaño, Nombre, Tipo
        private string _currentSortDirection = "Descendente"; // Descendente, Ascendente
        private bool _isApplyingFilter;

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    ApplyFilterAndSort();
                }
            }
        }

        public List<string> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

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
        public bool CanClean => !IsScanning && !IsCleaning && _allItems.Count > 0;

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilterAndSort();
                }
            }
        }

        public List<CleanableItem> DisplayItems
        {
            get => _displayItems;
            set => SetProperty(ref _displayItems, value);
        }

        public string TotalFoundSizeText
        {
            get => _totalFoundSizeText;
            set => SetProperty(ref _totalFoundSizeText, value);
        }

        public string SelectedSizeText
        {
            get => _selectedSizeText;
            set => SetProperty(ref _selectedSizeText, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string CurrentSortOption
        {
            get => _currentSortOption;
            set
            {
                if (SetProperty(ref _currentSortOption, value))
                {
                    ApplyFilterAndSort();
                }
            }
        }

        public string CurrentSortDirection
        {
            get => _currentSortDirection;
            set
            {
                if (SetProperty(ref _currentSortDirection, value))
                {
                    ApplyFilterAndSort();
                }
            }
        }

        public ICommand ScanCommand { get; }
        public ICommand CancelScanCommand { get; }
        public ICommand CleanCommand { get; }
        public ICommand ToggleAllSelectionCommand { get; }

        public CleanupViewModel(ICleanupManagerService cleanupManager, IConfigurationService configurationService)
        {
            _cleanupManager = cleanupManager ?? throw new ArgumentNullException(nameof(cleanupManager));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));

            ScanCommand = new AsyncRelayCommand(ScanAsync);
            CancelScanCommand = new RelayCommand(CancelScan);
            CleanCommand = new AsyncRelayCommand(CleanAsync);
            ToggleAllSelectionCommand = new RelayCommand<string>(ToggleAllSelection);
        }

        private async Task ScanAsync()
        {
            if (IsScanning || IsCleaning) return;

            IsScanning = true;
            Progress = 0;
            StatusMessage = "Escaneando el sistema en segundo plano...";
            _scanCts = new CancellationTokenSource();

            _allItems.Clear();
            DisplayItems = new List<CleanableItem>();
            TotalFoundSizeText = "0 Bytes";
            SelectedSizeText = "0 Bytes";
            _totalFoundSize = 0;
            _selectedSize = 0;
            OnPropertyChanged(nameof(CanClean));

            try
            {
                var progressIndicator = new Progress<double>(val => Progress = val);
                var scanResult = await _cleanupManager.ScanAllAsync(progressIndicator, _scanCts.Token);

                _allItems = scanResult.Items;
                _totalFoundSize = scanResult.TotalSize;
                TotalFoundSizeText = CleanableItem.FormatSize(_totalFoundSize);

                // Escuchar cambios de selección en tiempo real
                foreach (var item in _allItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }

                // Cargar categorías dinámicas encontradas
                var uniqueCategories = _allItems.Select(x => x.FileType).Distinct().OrderBy(x => x).ToList();
                var newList = new List<string> { "Todos" };
                newList.AddRange(uniqueCategories);
                Categories = newList;
                _selectedCategory = "Todos"; // Asignar a field para no lanzar ApplyFilterAndSort dos veces
                OnPropertyChanged(nameof(SelectedCategory));

                UpdateSelectedSize();
                ApplyFilterAndSort();
                OnPropertyChanged(nameof(CanClean));

                StatusMessage = $"Análisis completado. Encontrados {CleanableItem.FormatSize(_totalFoundSize)} en {_allItems.Count} elementos.";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Análisis cancelado por el usuario.";
                Progress = 0;
            }
            catch (Exception ex)
            {
                StatusMessage = "Ocurrió un error al analizar el sistema.";
                Log.Error(ex, "Error durante el análisis del CleanupViewModel.");
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

            var itemsToClean = _allItems.Where(x => x.IsSelected).ToList();
            if (itemsToClean.Count == 0)
            {
                MessageBox.Show("Por favor, seleccione al menos un elemento para limpiar.", "Ningún elemento seleccionado", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_configurationService.CurrentSettings.BypassRecycleBin)
            {
                var confirm1 = MessageBox.Show(
                    "¿Está seguro de que desea eliminar PERMANENTEMENTE los elementos seleccionados?\nEsta acción evitará la Papelera de reciclaje y no se podrá deshacer.",
                    "Confirmación de Eliminación Permanente (Paso 1 de 2)",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (confirm1 != MessageBoxResult.Yes) return;

                var confirm2 = MessageBox.Show(
                    "¿Está REALMENTE seguro de proceder con el borrado definitivo?\nSe destruirán de forma permanente los archivos seleccionados.",
                    "Confirmación Final (Paso 2 de 2)",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (confirm2 != MessageBoxResult.Yes) return;
            }

            IsCleaning = true;
            Progress = 0;
            StatusMessage = "Eliminando elementos de forma segura...";

            try
            {
                var progressIndicator = new Progress<double>(val => Progress = val);
                var token = CancellationToken.None;

                long sizeToClean = itemsToClean.Sum(x => x.Size);
                int cleanedCount = await _cleanupManager.CleanItemsAsync(itemsToClean, progressIndicator, token);

                // Registrar en el historial de limpiezas si se liberó espacio
                if (cleanedCount > 0 && sizeToClean > 0)
                {
                    _configurationService.CurrentSettings.CleanupHistory.Add(new CleanupHistoryItem
                    {
                        DateTime = DateTime.Now,
                        BytesCleaned = sizeToClean,
                        ItemsCount = cleanedCount
                    });
                    _configurationService.SaveSettings();
                }

                foreach (var cleanedItem in itemsToClean)
                {
                    cleanedItem.PropertyChanged -= Item_PropertyChanged;
                    _allItems.Remove(cleanedItem);
                }

                ApplyFilterAndSort();
                _totalFoundSize = _allItems.Sum(x => x.Size);
                TotalFoundSizeText = CleanableItem.FormatSize(_totalFoundSize);
                UpdateSelectedSize();
                OnPropertyChanged(nameof(CanClean));

                StatusMessage = $"Limpieza completada. Se eliminaron/procesaron {cleanedCount} elementos con éxito.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Ocurrió un error al limpiar los archivos.";
                Log.Error(ex, "Error durante la limpieza en CleanupViewModel.");
            }
            finally
            {
                IsCleaning = false;
            }
        }

        private void ToggleAllSelection(string? isSelectedStr)
        {
            if (bool.TryParse(isSelectedStr, out bool isSelected))
            {
                foreach (var item in _displayItems)
                {
                    item.IsSelected = isSelected;
                }
                UpdateSelectedSize();
            }
        }

        private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!_isApplyingFilter && e.PropertyName == nameof(CleanableItem.IsSelected))
            {
                UpdateSelectedSize();
            }
        }

        private void UpdateSelectedSize()
        {
            _selectedSize = _allItems.Where(x => x.IsSelected).Sum(x => x.Size);
            SelectedSizeText = CleanableItem.FormatSize(_selectedSize);
        }

        private void ApplyFilterAndSort()
        {
            IEnumerable<CleanableItem> query = _allItems;

            if (SelectedCategory != "Todos")
            {
                query = query.Where(x => x.FileType == SelectedCategory);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                                         x.Path.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (CurrentSortOption == "Tamaño")
            {
                query = CurrentSortDirection == "Descendente"
                    ? query.OrderByDescending(x => x.Size)
                    : query.OrderBy(x => x.Size);
            }
            else if (CurrentSortOption == "Nombre")
            {
                query = CurrentSortDirection == "Descendente"
                    ? query.OrderByDescending(x => x.Name)
                    : query.OrderBy(x => x.Name);
            }
            else if (CurrentSortOption == "Tipo")
            {
                query = CurrentSortDirection == "Descendente"
                    ? query.OrderByDescending(x => x.FileType)
                    : query.OrderBy(x => x.FileType);
            }

            _isApplyingFilter = true;
            try
            {
                DisplayItems = query.ToList();

                var displayedSet = new HashSet<CleanableItem>(DisplayItems);
                foreach (var item in _allItems)
                {
                    if (item.IsSelected && !displayedSet.Contains(item))
                    {
                        item.IsSelected = false;
                    }
                }
            }
            finally
            {
                _isApplyingFilter = false;
            }

            UpdateSelectedSize();
        }
    }
}
