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
    public class RuntimeInstallerViewModel : ViewModelBase
    {
        private readonly IRuntimeInstallerService _installerService;
        private CancellationTokenSource? _cts;

        private ObservableCollection<RuntimeItem> _runtimes = new();
        private bool _isInstalling;
        private double _progressValue;
        private string _statusMessage = "Selecciona las dependencias deseadas y haz clic en 'Instalar Seleccionados'.";
        private string _consoleLog = string.Empty;

        public ObservableCollection<RuntimeItem> Runtimes
        {
            get => _runtimes;
            set => SetProperty(ref _runtimes, value);
        }

        public bool IsInstalling
        {
            get => _isInstalling;
            set
            {
                if (SetProperty(ref _isInstalling, value))
                {
                    OnPropertyChanged(nameof(CanExecuteAction));
                }
            }
        }

        public bool CanExecuteAction => !IsInstalling;

        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

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

        public ICommand SelectAllCommand { get; }
        public ICommand DeselectAllCommand { get; }
        public ICommand InstallSelectedCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand RefreshCommand { get; }

        public RuntimeInstallerViewModel(IRuntimeInstallerService installerService)
        {
            _installerService = installerService ?? throw new ArgumentNullException(nameof(installerService));

            _ = LoadRuntimesAsync();

            SelectAllCommand = new RelayCommand(() => SetAllSelected(true));
            DeselectAllCommand = new RelayCommand(() => SetAllSelected(false));
            InstallSelectedCommand = new AsyncRelayCommand(InstallSelectedAsync, () => CanExecuteAction);
            CancelCommand = new RelayCommand(CancelInstallation, () => IsInstalling);
            ClearLogCommand = new RelayCommand(() => ConsoleLog = string.Empty);
            RefreshCommand = new AsyncRelayCommand(LoadRuntimesAsync);
        }

        private async Task LoadRuntimesAsync()
        {
            StatusMessage = "Comprobando dependencias instaladas...";
            var list = _installerService.GetAvailableRuntimes();

            var tasks = list.Select(async item =>
            {
                bool installed = await _installerService.IsRuntimeInstalledAsync(item.Id);
                item.Status = installed ? "Instalado" : "No Instalado";
                item.IsSelected = !installed;
            }).ToArray();

            await Task.WhenAll(tasks);

            Runtimes = new ObservableCollection<RuntimeItem>(list);
            StatusMessage = "Selecciona las dependencias deseadas y haz clic en 'Instalar Seleccionados'.";
        }

        private void SetAllSelected(bool selected)
        {
            foreach (var item in Runtimes)
            {
                item.IsSelected = selected;
            }
        }

        private async Task InstallSelectedAsync()
        {
            var selectedItems = Runtimes.Where(r => r.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                StatusMessage = "⚠️ No hay dependencias seleccionadas para instalar.";
                return;
            }

            IsInstalling = true;
            ProgressValue = 0;
            _cts = new CancellationTokenSource();

            AppendLog($"\n[{DateTime.Now:HH:mm:ss}] --- Iniciando proceso de instalación por lotes ({selectedItems.Count} paquetes) ---");

            int installedCount = 0;
            var progress = new Progress<string>(msg => AppendLog(msg));

            try
            {
                for (int i = 0; i < selectedItems.Count; i++)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    var item = selectedItems[i];
                    StatusMessage = $"Instalando [{i + 1}/{selectedItems.Count}]: {item.Name}...";

                    bool success = await _installerService.InstallRuntimeAsync(item, progress, _cts.Token);
                    if (success) installedCount++;

                    ProgressValue = ((double)(i + 1) / selectedItems.Count) * 100;
                }

                StatusMessage = $"✅ Proceso finalizado: {installedCount} de {selectedItems.Count} dependencias instaladas.";
                
                // Recargar el estado actual de las dependencias
                await LoadRuntimesAsync();
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Instalación por lotes cancelada.";
                AppendLog("\n[!] Proceso de instalación detenido por el usuario.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error durante la instalación por lotes de runtimes.");
                StatusMessage = $"Error durante el lote: {ex.Message}";
            }
            finally
            {
                IsInstalling = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void CancelInstallation()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                StatusMessage = "Cancelando instalaciones...";
                _cts.Cancel();
            }
        }

        private void AppendLog(string message)
        {
            ConsoleLog += message + Environment.NewLine;
        }
    }
}
