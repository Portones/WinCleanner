using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.ViewModels
{
    public class TcpTweakerViewModel : ViewModelBase
    {
        private readonly ITcpTweakerService _tcpService;

        private bool _isAutoTuningEnabled;
        private bool _isNagleDisabled;
        private bool _isChimneyOffloadEnabled;
        private bool _isLoading;
        private string _statusMessage = "Cargando estado actual de la pila TCP/IP...";

        public bool IsAutoTuningEnabled
        {
            get => _isAutoTuningEnabled;
            set => SetProperty(ref _isAutoTuningEnabled, value);
        }

        public bool IsNagleDisabled
        {
            get => _isNagleDisabled;
            set => SetProperty(ref _isNagleDisabled, value);
        }

        public bool IsChimneyOffloadEnabled
        {
            get => _isChimneyOffloadEnabled;
            set => SetProperty(ref _isChimneyOffloadEnabled, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand LoadStateCommand { get; }
        public ICommand ToggleAutoTuningCommand { get; }
        public ICommand ToggleNagleCommand { get; }
        public ICommand ToggleChimneyCommand { get; }
        public ICommand ApplyRecommendedCommand { get; }

        public TcpTweakerViewModel(ITcpTweakerService tcpService)
        {
            _tcpService = tcpService ?? throw new ArgumentNullException(nameof(tcpService));

            LoadStateCommand = new AsyncRelayCommand(LoadStateAsync);
            ToggleAutoTuningCommand = new AsyncRelayCommand(ToggleAutoTuningAsync);
            ToggleNagleCommand = new AsyncRelayCommand(ToggleNagleAsync);
            ToggleChimneyCommand = new AsyncRelayCommand(ToggleChimneyAsync);
            ApplyRecommendedCommand = new AsyncRelayCommand(ApplyRecommendedAsync);

            _ = LoadStateAsync();
        }

        private async Task LoadStateAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            StatusMessage = "Consultando parámetros TCP/IP del sistema...";

            try
            {
                IsAutoTuningEnabled = await _tcpService.IsTcpAutoTuningEnabledAsync();
                IsNagleDisabled = await _tcpService.IsNagleAlgorithmDisabledAsync();
                IsChimneyOffloadEnabled = await _tcpService.IsTcpChimneyOffloadEnabledAsync();

                StatusMessage = "✅ Configuración TCP/IP cargada correctamente.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al cargar estado de TCP Tweaker.");
                StatusMessage = $"Error al leer configuración: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ToggleAutoTuningAsync()
        {
            IsLoading = true;
            bool targetState = !IsAutoTuningEnabled;
            StatusMessage = targetState ? "Activando TCP Auto-Tuning..." : "Desactivando TCP Auto-Tuning...";

            bool success = await _tcpService.SetTcpAutoTuningAsync(targetState);
            if (success)
            {
                IsAutoTuningEnabled = targetState;
                StatusMessage = $"✅ TCP Auto-Tuning {(targetState ? "activado (Normal)" : "desactivado")}.";
            }
            else
            {
                StatusMessage = "❌ No se pudo cambiar la configuración de TCP Auto-Tuning.";
            }

            IsLoading = false;
        }

        private async Task ToggleNagleAsync()
        {
            IsLoading = true;
            bool targetState = !IsNagleDisabled;
            StatusMessage = targetState ? "Deshabilitando Algoritmo de Nagle (Baja Latencia)..." : "Habilitando Algoritmo de Nagle...";

            bool success = await _tcpService.SetNagleAlgorithmDisabledAsync(targetState);
            if (success)
            {
                IsNagleDisabled = targetState;
                StatusMessage = $"✅ Algoritmo de Nagle {(targetState ? "deshabilitado (Latencia Optimizada)" : "restaurado por defecto")}.";
            }
            else
            {
                StatusMessage = "❌ No se pudo modificar el registro para el Algoritmo de Nagle. Requiere permisos elevados.";
            }

            IsLoading = false;
        }

        private async Task ToggleChimneyAsync()
        {
            IsLoading = true;
            bool targetState = !IsChimneyOffloadEnabled;
            StatusMessage = targetState ? "Activando TCP Chimney Offload..." : "Desactivando TCP Chimney Offload...";

            bool success = await _tcpService.SetTcpChimneyOffloadAsync(targetState);
            if (success)
            {
                IsChimneyOffloadEnabled = targetState;
                StatusMessage = $"✅ TCP Chimney Offload {(targetState ? "activado" : "desactivado")}.";
            }
            else
            {
                StatusMessage = "❌ No se pudo cambiar TCP Chimney Offload.";
            }

            IsLoading = false;
        }

        private async Task ApplyRecommendedAsync()
        {
            IsLoading = true;
            StatusMessage = "Aplicando perfil de optimización de red recomendado para Gaming y Streaming...";

            try
            {
                await _tcpService.SetTcpAutoTuningAsync(true);
                await _tcpService.SetNagleAlgorithmDisabledAsync(true);
                await _tcpService.SetTcpChimneyOffloadAsync(true);

                await LoadStateAsync();
                StatusMessage = "⚡ Perfil de optimización TCP/IP recomendado aplicado exitosamente.";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al aplicar perfil recomendado TCP.");
                StatusMessage = $"Error al aplicar perfil: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
