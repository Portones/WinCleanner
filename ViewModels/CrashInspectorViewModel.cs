using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.ViewModels
{
    public class CrashInspectorViewModel : ViewModelBase
    {
        private readonly ICrashInspectorService _crashService;
        private CancellationTokenSource? _cts;

        private List<CrashItem> _allCrashes = new();
        private ObservableCollection<CrashItem> _crashes = new();
        private CrashItem? _selectedCrash;
        private bool _isLoading;
        private string _searchText = string.Empty;
        private int _filterType = 0; // 0: Todos, 1: Solo Críticos/BSOD, 2: Errores App
        private string _statusMessage = "Pulsa 'Cargar Incidentes' para analizar la bitácora de Windows.";

        public ObservableCollection<CrashItem> Crashes
        {
            get => _crashes;
            set => SetProperty(ref _crashes, value);
        }

        public CrashItem? SelectedCrash
        {
            get => _selectedCrash;
            set => SetProperty(ref _selectedCrash, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
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

        public int FilterType
        {
            get => _filterType;
            set
            {
                if (SetProperty(ref _filterType, value))
                {
                    ApplyFilter();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public int TotalCrashesCount => _allCrashes.Count;
        public int CriticalCrashesCount => _allCrashes.Count(c => c.IsCritical);

        public ICommand LoadCrashesCommand { get; }
        public ICommand ClearSearchCommand { get; }

        public CrashInspectorViewModel(ICrashInspectorService crashService)
        {
            _crashService = crashService ?? throw new ArgumentNullException(nameof(crashService));

            LoadCrashesCommand = new AsyncRelayCommand(LoadCrashesAsync);
            ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
        }

        private async Task LoadCrashesAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            StatusMessage = "Analizando el registro de eventos de Windows en busca de fallos...";
            _cts = new CancellationTokenSource();

            try
            {
                _allCrashes = await _crashService.GetRecentCrashesAsync(100, _cts.Token);
                ApplyFilter();

                OnPropertyChanged(nameof(TotalCrashesCount));
                OnPropertyChanged(nameof(CriticalCrashesCount));

                StatusMessage = _allCrashes.Count == 0
                    ? "✅ No se detectaron fallos ni incidentes recientes en el registro."
                    : $"✅ Se analizaron {_allCrashes.Count} incidentes ({CriticalCrashesCount} críticos).";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Análisis cancelado.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al cargar incidentes de la bitácora.");
                StatusMessage = $"Error al cargar incidentes: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilter()
        {
            var query = _allCrashes.AsEnumerable();

            if (FilterType == 1) // Solo Críticos
            {
                query = query.Where(c => c.IsCritical);
            }
            else if (FilterType == 2) // Errores App
            {
                query = query.Where(c => !c.IsCritical);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(c =>
                    c.Source.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.EventId.ToString().Contains(SearchText));
            }

            Crashes = new ObservableCollection<CrashItem>(query);
        }
    }
}
