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
    public class SsdOptimizerViewModel : ViewModelBase
    {
        private readonly ISsdOptimizerService _ssdService;
        private CancellationTokenSource? _cts;

        private ObservableCollection<DriveMediumInfo> _drives = new();
        private DriveMediumInfo? _selectedDrive;
        private bool _isOptimizing;
        private string _statusMessage = "Cargando unidades de almacenamiento del sistema...";
        private string _consoleLog = string.Empty;

        public ObservableCollection<DriveMediumInfo> Drives
        {
            get => _drives;
            set => SetProperty(ref _drives, value);
        }

        public DriveMediumInfo? SelectedDrive
        {
            get => _selectedDrive;
            set => SetProperty(ref _selectedDrive, value);
        }

        public bool IsOptimizing
        {
            get => _isOptimizing;
            set
            {
                if (SetProperty(ref _isOptimizing, value))
                {
                    OnPropertyChanged(nameof(CanExecuteAction));
                }
            }
        }

        public bool CanExecuteAction => !IsOptimizing;

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string ConsoleLog
        {
            get => _consoleLog;
            set => SetProperty(ref _consoleLog, value);
        }

        public ICommand ScanDrivesCommand { get; }
        public ICommand OptimizeDriveCommand { get; }
        public ICommand OptimizeAllSsdCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ClearLogCommand { get; }

        public SsdOptimizerViewModel(ISsdOptimizerService ssdService)
        {
            _ssdService = ssdService ?? throw new ArgumentNullException(nameof(ssdService));

            ScanDrivesCommand = new AsyncRelayCommand(ScanDrivesAsync);
            OptimizeDriveCommand = new AsyncRelayCommand(OptimizeSelectedDriveAsync, () => CanExecuteAction && SelectedDrive != null);
            OptimizeAllSsdCommand = new AsyncRelayCommand(OptimizeAllSsdAsync, () => CanExecuteAction);
            CancelCommand = new RelayCommand(CancelOptimization, () => IsOptimizing);
            ClearLogCommand = new RelayCommand(() => ConsoleLog = string.Empty);

            _ = ScanDrivesAsync();
        }

        private async Task ScanDrivesAsync()
        {
            if (IsOptimizing) return;
            StatusMessage = "Analizando unidades de almacenamiento WMI...";

            try
            {
                var list = await _ssdService.GetStorageDrivesAsync();
                Drives = new ObservableCollection<DriveMediumInfo>(list);

                if (Drives.Count > 0) SelectedDrive = Drives[0];

                int ssdCount = Drives.Count(d => d.IsSsd);
                StatusMessage = $"✅ Se detectaron {Drives.Count} unidades ({ssdCount} de tipo SSD).";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al escanear unidades de almacenamiento.");
                StatusMessage = $"Error al detectar unidades: {ex.Message}";
            }
        }

        private async Task OptimizeSelectedDriveAsync()
        {
            if (SelectedDrive == null || IsOptimizing) return;

            IsOptimizing = true;
            _cts = new CancellationTokenSource();
            StatusMessage = $"Optimizando unidad {SelectedDrive.DriveLetter}...";

            var progress = new Progress<string>(text => AppendLog(text));

            try
            {
                await _ssdService.OptimizeDriveAsync(SelectedDrive, progress, _cts.Token);
                StatusMessage = $"Operación finalizada para {SelectedDrive.DriveLetter}.";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Optimización cancelada.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al optimizar unidad seleccionada.");
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsOptimizing = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task OptimizeAllSsdAsync()
        {
            var ssdDrives = Drives.Where(d => d.IsSsd).ToList();
            if (ssdDrives.Count == 0 || IsOptimizing)
            {
                StatusMessage = "⚠️ No hay unidades de tipo SSD detectadas para optimizar.";
                return;
            }

            IsOptimizing = true;
            _cts = new CancellationTokenSource();
            StatusMessage = "Iniciando optimización TRIM por lotes para todas las unidades SSD...";
            var progress = new Progress<string>(text => AppendLog(text));

            try
            {
                for (int i = 0; i < ssdDrives.Count; i++)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var drive = ssdDrives[i];

                    StatusMessage = $"Optimizando unidad [{i + 1}/{ssdDrives.Count}]: {drive.DriveLetter}...";
                    await _ssdService.OptimizeDriveAsync(drive, progress, _cts.Token);
                }

                StatusMessage = "✅ Optimización por lote de unidades SSD completada.";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Optimización por lote cancelada.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error durante la optimización por lote de SSD.");
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsOptimizing = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void CancelOptimization()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                StatusMessage = "Cancelando optimizaciones...";
                _cts.Cancel();
            }
        }

        private void AppendLog(string text)
        {
            ConsoleLog += text + Environment.NewLine;
        }
    }
}
