using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.ViewModels
{
    public class SystemRepairViewModel : ViewModelBase
    {
        private readonly ISystemRepairService _repairService;
        private CancellationTokenSource? _cts;

        private bool _isRunning;
        private string _currentOperation = "En espera - Seleccione una herramienta de reparación.";
        private double _progressValue;
        private string _outputLog = string.Empty;

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    OnPropertyChanged(nameof(CanExecuteAction));
                }
            }
        }

        public bool CanExecuteAction => !IsRunning;

        public string CurrentOperation
        {
            get => _currentOperation;
            set => SetProperty(ref _currentOperation, value);
        }

        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public string OutputLog
        {
            get => _outputLog;
            set => SetProperty(ref _outputLog, value);
        }

        public ICommand RunSfcCommand { get; }
        public ICommand RunDismCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ClearOutputCommand { get; }

        public SystemRepairViewModel(ISystemRepairService repairService)
        {
            _repairService = repairService ?? throw new ArgumentNullException(nameof(repairService));

            RunSfcCommand = new AsyncRelayCommand(RunSfcAsync, () => CanExecuteAction);
            RunDismCommand = new AsyncRelayCommand(RunDismAsync, () => CanExecuteAction);
            CancelCommand = new RelayCommand(CancelOperation, () => IsRunning);
            ClearOutputCommand = new RelayCommand(ClearOutput);
        }

        private async Task RunSfcAsync()
        {
            if (IsRunning) return;

            IsRunning = true;
            ProgressValue = 0;
            CurrentOperation = "Ejecutando Comprobador de Archivos de Sistema (SFC)...";
            _cts = new CancellationTokenSource();

            AppendLog($"\n[{DateTime.Now:HH:mm:ss}] Iniciando SFC Scannow...\n");

            try
            {
                var outputProgress = new Progress<string>(text => AppendLog(text));
                var percentProgress = new Progress<double>(p => ProgressValue = p);

                await _repairService.RunSfcScanAsync(outputProgress, percentProgress, _cts.Token);
                CurrentOperation = "Proceso SFC finalizado.";
            }
            catch (OperationCanceledException)
            {
                CurrentOperation = "Operación SFC cancelada.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error durante la ejecución de SFC.");
                AppendLog($"\n[ERROR] {ex.Message}");
                CurrentOperation = "Error en la ejecución de SFC.";
            }
            finally
            {
                IsRunning = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task RunDismAsync()
        {
            if (IsRunning) return;

            IsRunning = true;
            ProgressValue = 0;
            CurrentOperation = "Ejecutando Reparación de Imagen DISM...";
            _cts = new CancellationTokenSource();

            AppendLog($"\n[{DateTime.Now:HH:mm:ss}] Iniciando DISM RestoreHealth...\n");

            try
            {
                var outputProgress = new Progress<string>(text => AppendLog(text));
                var percentProgress = new Progress<double>(p => ProgressValue = p);

                await _repairService.RunDismRepairAsync(outputProgress, percentProgress, _cts.Token);
                CurrentOperation = "Proceso DISM finalizado.";
            }
            catch (OperationCanceledException)
            {
                CurrentOperation = "Operación DISM cancelada.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error durante la ejecución de DISM.");
                AppendLog($"\n[ERROR] {ex.Message}");
                CurrentOperation = "Error en la ejecución de DISM.";
            }
            finally
            {
                IsRunning = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void CancelOperation()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                CurrentOperation = "Cancelando operación...";
                _cts.Cancel();
            }
        }

        private void ClearOutput()
        {
            OutputLog = string.Empty;
        }

        private void AppendLog(string line)
        {
            OutputLog += line + Environment.NewLine;
        }
    }
}
