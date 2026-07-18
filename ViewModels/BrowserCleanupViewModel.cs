using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class BrowserCleanupViewModel : ViewModelBase
    {
        private readonly IBrowserCleanupService _browserService;
        private readonly IEventLogCleanerService _eventLogService;

        // ─── Browser ───────────────────────────────────────────────────────────
        private ObservableCollection<BrowserProfile> _browserProfiles = new();
        private bool _isScanning;
        private bool _isCleaning;
        private string _scanStatus = "Pulsa \"Escanear\" para detectar la caché de todos los navegadores instalados.";
        private double _cleanProgress;

        // ─── Event Logs ────────────────────────────────────────────────────────
        private ObservableCollection<EventLogInfo> _eventLogs = new();
        private bool _isLoadingLogs;
        private bool _isClearingLogs;
        private string _logStatus = "Pulsa \"Cargar Registros\" para ver los registros de eventos de Windows.";
        private double _logProgress;
        private int _activeTab; // 0 = Navegadores, 1 = Registros de Eventos

        private CancellationTokenSource? _cts;

        // ─── Properties ────────────────────────────────────────────────────────
        public ObservableCollection<BrowserProfile> BrowserProfiles
        {
            get => _browserProfiles;
            set => SetProperty(ref _browserProfiles, value);
        }

        public bool IsScanning
        {
            get => _isScanning;
            set => SetProperty(ref _isScanning, value);
        }

        public bool IsCleaning
        {
            get => _isCleaning;
            set => SetProperty(ref _isCleaning, value);
        }

        public string ScanStatus
        {
            get => _scanStatus;
            set => SetProperty(ref _scanStatus, value);
        }

        public double CleanProgress
        {
            get => _cleanProgress;
            set => SetProperty(ref _cleanProgress, value);
        }

        public ObservableCollection<EventLogInfo> EventLogs
        {
            get => _eventLogs;
            set => SetProperty(ref _eventLogs, value);
        }

        public bool IsLoadingLogs
        {
            get => _isLoadingLogs;
            set => SetProperty(ref _isLoadingLogs, value);
        }

        public bool IsClearingLogs
        {
            get => _isClearingLogs;
            set => SetProperty(ref _isClearingLogs, value);
        }

        public string LogStatus
        {
            get => _logStatus;
            set => SetProperty(ref _logStatus, value);
        }

        public double LogProgress
        {
            get => _logProgress;
            set => SetProperty(ref _logProgress, value);
        }

        public int ActiveTab
        {
            get => _activeTab;
            set => SetProperty(ref _activeTab, value);
        }

        public long TotalCacheSizeBytes => _browserProfiles.Sum(p => p.CacheSizeBytes);
        public string TotalCacheSizeText => CleanableItem.FormatSize(TotalCacheSizeBytes);

        // ─── Commands ──────────────────────────────────────────────────────────
        public ICommand ScanCommand         { get; }
        public ICommand CleanCommand        { get; }
        public ICommand LoadLogsCommand     { get; }
        public ICommand ClearLogsCommand    { get; }
        public ICommand SelectAllLogsCommand   { get; }
        public ICommand DeselectAllLogsCommand { get; }

        public BrowserCleanupViewModel(
            IBrowserCleanupService browserService,
            IEventLogCleanerService eventLogService)
        {
            _browserService  = browserService  ?? throw new ArgumentNullException(nameof(browserService));
            _eventLogService = eventLogService ?? throw new ArgumentNullException(nameof(eventLogService));

            ScanCommand            = new AsyncRelayCommand(ScanAsync);
            CleanCommand           = new AsyncRelayCommand(CleanAsync);
            LoadLogsCommand        = new AsyncRelayCommand(LoadLogsAsync);
            ClearLogsCommand       = new AsyncRelayCommand(ClearLogsAsync);
            SelectAllLogsCommand   = new RelayCommand(() => SetAllLogs(true));
            DeselectAllLogsCommand = new RelayCommand(() => SetAllLogs(false));
        }

        // ─── Browser methods ───────────────────────────────────────────────────
        private async Task ScanAsync()
        {
            if (IsScanning) return;
            IsScanning = true;
            BrowserProfiles.Clear();
            ScanStatus = "Escaneando navegadores instalados...";
            CleanProgress = 0;
            _cts = new CancellationTokenSource();

            try
            {
                var progress = new Progress<string>(msg => ScanStatus = msg);
                var profiles = await _browserService.ScanBrowserCacheAsync(progress, _cts.Token);
                BrowserProfiles = new ObservableCollection<BrowserProfile>(profiles);
                OnPropertyChanged(nameof(TotalCacheSizeBytes));
                OnPropertyChanged(nameof(TotalCacheSizeText));

                ScanStatus = profiles.Count == 0
                    ? "✅ No se encontró caché acumulada en ningún navegador."
                    : $"✅ Encontrado {profiles.Count} perfiles — {TotalCacheSizeText} de caché total.";
            }
            catch (OperationCanceledException)
            {
                ScanStatus = "Escaneo cancelado.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al escanear la caché de navegadores.");
                ScanStatus = $"Error al escanear: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        private async Task CleanAsync()
        {
            if (IsCleaning || BrowserProfiles.Count == 0) return;

            var result = MessageBox.Show(
                $"Se va a limpiar la caché de {BrowserProfiles.Count} perfiles ({TotalCacheSizeText}).\n\n" +
                "Asegúrese de cerrar todos los navegadores antes de continuar.\n\n¿Continuar?",
                "Limpiar Caché de Navegadores", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsCleaning = true;
            CleanProgress = 0;
            ScanStatus = "Limpiando caché de navegadores...";
            _cts = new CancellationTokenSource();

            try
            {
                var progress = new Progress<double>(p =>
                {
                    CleanProgress = p;
                    ScanStatus = $"Limpiando... {p:F0}%";
                });

                long freed = await _browserService.CleanBrowserCacheAsync(BrowserProfiles, progress, _cts.Token);
                CleanProgress = 100;
                ScanStatus = $"✅ Limpieza completada — {CleanableItem.FormatSize(freed)} liberados.";
                BrowserProfiles.Clear();
                OnPropertyChanged(nameof(TotalCacheSizeBytes));
                OnPropertyChanged(nameof(TotalCacheSizeText));
            }
            catch (OperationCanceledException)
            {
                ScanStatus = "Limpieza cancelada.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al limpiar la caché de navegadores.");
                ScanStatus = $"Error: {ex.Message}";
            }
            finally
            {
                IsCleaning = false;
            }
        }

        // ─── Event Log methods ─────────────────────────────────────────────────
        private async Task LoadLogsAsync()
        {
            if (IsLoadingLogs) return;
            IsLoadingLogs = true;
            EventLogs.Clear();
            LogStatus = "Cargando registros de eventos de Windows...";
            _cts = new CancellationTokenSource();

            try
            {
                var logs = await _eventLogService.GetEventLogsAsync(_cts.Token);
                EventLogs = new ObservableCollection<EventLogInfo>(logs);
                LogStatus = $"✅ {logs.Count} registros cargados.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al cargar los registros de eventos.");
                LogStatus = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoadingLogs = false;
            }
        }

        private async Task ClearLogsAsync()
        {
            var selected = EventLogs.Where(l => l.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("No hay registros seleccionados.", "Limpiar Registros",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Se van a vaciar {selected.Count} registros de eventos de Windows.\n\n" +
                "Esta acción no puede deshacerse. ¿Continuar?",
                "Confirmar Limpieza", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsClearingLogs = true;
            LogProgress = 0;
            LogStatus = "Vaciando registros de eventos...";
            _cts = new CancellationTokenSource();

            try
            {
                var progress = new Progress<double>(p =>
                {
                    LogProgress = p;
                    LogStatus = $"Vaciando... {p:F0}%";
                });

                int cleared = await _eventLogService.ClearEventLogsAsync(selected, progress, _cts.Token);
                LogProgress = 100;
                LogStatus = $"✅ {cleared} registros de eventos vaciados correctamente.";
                await LoadLogsAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al vaciar los registros de eventos.");
                LogStatus = $"Error: {ex.Message}";
            }
            finally
            {
                IsClearingLogs = false;
            }
        }

        private void SetAllLogs(bool selected)
        {
            foreach (var log in EventLogs)
                log.IsSelected = selected;
            // Forzar refresco de la UI
            EventLogs = new ObservableCollection<EventLogInfo>(EventLogs);
        }
    }
}
