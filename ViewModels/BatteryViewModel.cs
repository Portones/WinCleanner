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
    public class BatteryViewModel : ViewModelBase
    {
        private readonly IBatteryService _batteryService;
        private DispatcherTimer _timer;

        private BatteryInfo _currentBattery = new();
        private ObservableCollection<PowerPlanItem> _powerPlans = new();
        private PowerPlanItem? _selectedPowerPlan;
        private string _statusMessage = "Listo";
        private bool _isLoading;
        private ObservableCollection<string> _tips = new();

        public BatteryInfo CurrentBattery
        {
            get => _currentBattery;
            set
            {
                if (SetProperty(ref _currentBattery, value))
                {
                    OnPropertyChanged(nameof(HasBattery));
                }
            }
        }

        public bool HasBattery => CurrentBattery?.HasBattery ?? false;

        public ObservableCollection<PowerPlanItem> PowerPlans
        {
            get => _powerPlans;
            set => SetProperty(ref _powerPlans, value);
        }

        public PowerPlanItem? SelectedPowerPlan
        {
            get => _selectedPowerPlan;
            set
            {
                if (SetProperty(ref _selectedPowerPlan, value) && value != null)
                {
                    _ = ChangePowerPlanAsync(value);
                }
            }
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

        public ObservableCollection<string> Tips
        {
            get => _tips;
            set => SetProperty(ref _tips, value);
        }

        public ICommand RefreshCommand { get; }

        public BatteryViewModel(IBatteryService batteryService)
        {
            _batteryService = batteryService ?? throw new ArgumentNullException(nameof(batteryService));

            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += async (s, e) => await LoadBatteryInfoOnlyAsync();

            _ = LoadDataAsync();
            _timer.Start();
        }

        private async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                // Cargar Info de Batería
                var info = await _batteryService.GetBatteryInfoAsync();
                CurrentBattery = info;

                if (info.HasBattery)
                {
                    // Cargar Planes de Energía
                    var plans = await _batteryService.GetPowerPlansAsync();
                    PowerPlans = new ObservableCollection<PowerPlanItem>(plans);

                    // Seleccionar plan activo sin disparar el setter con bucles
                    var active = plans.FirstOrDefault(p => p.IsActive);
                    _selectedPowerPlan = active;
                    OnPropertyChanged(nameof(SelectedPowerPlan));

                    GenerateBatteryTips(info);
                }

                StatusMessage = $"Última actualización completa: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error al leer datos de energía.";
                Log.Error(ex, "Error al cargar datos en BatteryViewModel.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadBatteryInfoOnlyAsync()
        {
            try
            {
                var info = await _batteryService.GetBatteryInfoAsync();
                CurrentBattery = info;

                if (info.HasBattery)
                {
                    GenerateBatteryTips(info);
                }

                StatusMessage = $"Última actualización de batería: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error en el auto-refresco de información de batería.");
            }
        }

        private async Task ChangePowerPlanAsync(PowerPlanItem plan)
        {
            try
            {
                StatusMessage = $"Cambiando plan a {plan.Name}...";
                bool success = await _batteryService.SetActivePowerPlanAsync(plan.Guid);
                if (success)
                {
                    // Actualizar el estado de IsActive localmente
                    foreach (var p in PowerPlans)
                    {
                        p.IsActive = p.Guid == plan.Guid;
                    }
                    StatusMessage = $"Plan cambiado con éxito a {plan.Name}.";
                }
                else
                {
                    StatusMessage = "No se pudo activar el plan seleccionado.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error al alternar plan.";
                Log.Error(ex, "Error al cambiar plan de energía.");
            }
        }

        private void GenerateBatteryTips(BatteryInfo info)
        {
            var list = new List<string>();

            if (info.IsCharging)
            {
                list.Add("🔋 El dispositivo está conectado a la corriente. Puedes utilizar el plan de 'Alto Rendimiento' para máxima potencia.");
                list.Add("💡 Mantener la batería entre 20% y 80% ayuda a prolongar la vida útil de las celdas de iones de litio.");
            }
            else
            {
                if (info.ChargePercentage < 30)
                {
                    list.Add("⚠️ Nivel de carga bajo. Te sugerimos activar el plan de energía 'Economizador' y reducir el brillo de la pantalla.");
                }
                else
                {
                    list.Add("⚡ El dispositivo está en modo portátil. Te sugerimos utilizar el plan 'Equilibrado' para lograr un balance óptimo.");
                }
            }

            if (info.WearLevel > 15)
            {
                list.Add($"📉 Desgaste térmico de {info.WearLevel:F1}% detectado. La capacidad máxima real es menor a la original de fábrica.");
            }
            else
            {
                list.Add("✨ La salud de la batería es sobresaliente. No se observan signos de degradación en la capacidad.");
            }

            list.Add("💡 Desactiva Bluetooth, Wi-Fi (si no se usa) y periféricos innecesarios para extender la autonomía útil.");

            Tips = new ObservableCollection<string>(list);
        }

        public void StopTimer()
        {
            _timer.Stop();
        }

        public void StartTimer()
        {
            _timer.Start();
            _ = LoadDataAsync();
        }
    }
}
