using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.ViewModels
{
    public class TemperatureViewModel : ViewModelBase
    {
        private readonly ITemperatureService _tempService;
        private DispatcherTimer _timer;

        private ObservableCollection<TemperatureItem> _temperatures = new();
        private string _statusMessage = "Listo";
        private bool _isLoading;
        private bool _isAutoRefreshEnabled = true;

        private double _cpuTemp;
        private double _gpuTemp;
        private double _diskTemp;

        private string _globalWarningText = string.Empty;
        private bool _showGlobalWarning;

        public ObservableCollection<TemperatureItem> Temperatures
        {
            get => _temperatures;
            set => SetProperty(ref _temperatures, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsAutoRefreshEnabled
        {
            get => _isAutoRefreshEnabled;
            set
            {
                if (SetProperty(ref _isAutoRefreshEnabled, value))
                {
                    if (value) _timer.Start();
                    else _timer.Stop();
                }
            }
        }

        public double CpuTemp
        {
            get => _cpuTemp;
            set => SetProperty(ref _cpuTemp, value);
        }

        public double GpuTemp
        {
            get => _gpuTemp;
            set => SetProperty(ref _gpuTemp, value);
        }

        public double DiskTemp
        {
            get => _diskTemp;
            set => SetProperty(ref _diskTemp, value);
        }

        public string GlobalWarningText
        {
            get => _globalWarningText;
            set => SetProperty(ref _globalWarningText, value);
        }

        public bool ShowGlobalWarning
        {
            get => _showGlobalWarning;
            set => SetProperty(ref _showGlobalWarning, value);
        }

        public ICommand RefreshCommand { get; }

        public TemperatureViewModel(ITemperatureService tempService)
        {
            _tempService = tempService ?? throw new ArgumentNullException(nameof(tempService));

            RefreshCommand = new AsyncRelayCommand(LoadTemperaturesAsync);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _timer.Tick += async (s, e) => await LoadTemperaturesAsync();

            _ = LoadTemperaturesAsync();
            _timer.Start();
        }

        private async Task LoadTemperaturesAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                var temps = await _tempService.GetTemperaturesAsync();
                Temperatures = new ObservableCollection<TemperatureItem>(temps);

                // Asignar variables individuales para los Gauges
                var cpuItem = temps.FirstOrDefault(t => t.ComponentName.Contains("CPU"));
                if (cpuItem != null) CpuTemp = cpuItem.CurrentValue;

                var gpuItem = temps.FirstOrDefault(t => t.ComponentName.Contains("GPU"));
                if (gpuItem != null) GpuTemp = gpuItem.CurrentValue;

                var diskItem = temps.FirstOrDefault(t => t.ComponentName.Contains("SSD/HDD"));
                if (diskItem != null) DiskTemp = diskItem.CurrentValue;

                // Validar si hay algún componente sobrecalentado
                ValidateSystemThermalStatus(temps);

                StatusMessage = $"Última actualización: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error al leer las temperaturas.";
                Log.Error(ex, "Error al cargar temperaturas en TemperatureViewModel.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ValidateSystemThermalStatus(List<TemperatureItem> temps)
        {
            var hotComponents = temps.Where(t => t.Status == "Caliente").ToList();
            if (hotComponents.Count > 0)
            {
                var names = string.Join(", ", hotComponents.Select(c => c.ComponentName));
                GlobalWarningText = $"⚠️ ¡ATENCIÓN! Temperatura crítica detectada en: {names}. Se recomienda liberar carga de procesamiento.";
                ShowGlobalWarning = true;
            }
            else
            {
                var warmComponents = temps.Where(t => t.Status == "Templado").ToList();
                if (warmComponents.Count > 0)
                {
                    var names = string.Join(", ", warmComponents.Select(c => c.ComponentName));
                    GlobalWarningText = $"💡 Nota: {names} presenta temperaturas elevadas de trabajo. El sistema está vigilando.";
                    ShowGlobalWarning = true;
                }
                else
                {
                    ShowGlobalWarning = false;
                }
            }
        }

        public void StopTimer()
        {
            _timer.Stop();
        }

        public void StartTimer()
        {
            if (IsAutoRefreshEnabled)
            {
                _timer.Start();
            }
            _ = LoadTemperaturesAsync();
        }
    }
}
