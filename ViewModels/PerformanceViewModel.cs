using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.ViewModels
{
    public class PerformanceViewModel : ViewModelBase
    {
        private readonly IPerformanceService _performanceService;

        private bool _isTelemetryDisabled;
        private bool _isErrorReportingDisabled;
        private bool _isGameModeEnabled;
        private string _activePowerScheme = "Cargando...";
        private string _activeDns = "Cargando...";
        private string _statusMessage = "Listo";
        private bool _isLoadingDns;
        private ObservableCollection<DnsServerItem> _dnsServers = new();
        private double _internetSpeedMbps = -1;
        private bool _isSpeedTesting;
        private string _speedTestStatusText = "Sin probar";

        public bool IsTelemetryDisabled
        {
            get => _isTelemetryDisabled;
            set => SetProperty(ref _isTelemetryDisabled, value);
        }

        public bool IsErrorReportingDisabled
        {
            get => _isErrorReportingDisabled;
            set => SetProperty(ref _isErrorReportingDisabled, value);
        }

        public bool IsGameModeEnabled
        {
            get => _isGameModeEnabled;
            set => SetProperty(ref _isGameModeEnabled, value);
        }

        public string ActivePowerScheme
        {
            get => _activePowerScheme;
            set => SetProperty(ref _activePowerScheme, value);
        }

        public string ActiveDns
        {
            get => _activeDns;
            set => SetProperty(ref _activeDns, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsLoadingDns
        {
            get => _isLoadingDns;
            set => SetProperty(ref _isLoadingDns, value);
        }

        public ObservableCollection<DnsServerItem> DnsServers
        {
            get => _dnsServers;
            set => SetProperty(ref _dnsServers, value);
        }

        public double InternetSpeedMbps
        {
            get => _internetSpeedMbps;
            set => SetProperty(ref _internetSpeedMbps, value);
        }

        public bool IsSpeedTesting
        {
            get => _isSpeedTesting;
            set => SetProperty(ref _isSpeedTesting, value);
        }

        public string SpeedTestStatusText
        {
            get => _speedTestStatusText;
            set => SetProperty(ref _speedTestStatusText, value);
        }

        public ICommand ToggleTelemetryCommand { get; }
        public ICommand ToggleErrorReportingCommand { get; }
        public ICommand ToggleGameModeCommand { get; }
        public ICommand RunDnsTestCommand { get; }
        public ICommand ApplyDnsCommand { get; }
        public ICommand ApplyFastestDnsCommand { get; }
        public ICommand FlushDnsCommand { get; }
        public ICommand RunSpeedTestCommand { get; }

        public PerformanceViewModel(IPerformanceService performanceService)
        {
            _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));

            ToggleTelemetryCommand = new AsyncRelayCommand(ToggleTelemetryAsync);
            ToggleErrorReportingCommand = new AsyncRelayCommand(ToggleErrorReportingAsync);
            ToggleGameModeCommand = new AsyncRelayCommand(ToggleGameModeAsync);
            RunDnsTestCommand = new AsyncRelayCommand(RunDnsTestAsync);
            ApplyDnsCommand = new AsyncRelayCommand<DnsServerItem>(ApplyDnsAsync);
            ApplyFastestDnsCommand = new AsyncRelayCommand(ApplyFastestDnsAsync);
            FlushDnsCommand = new AsyncRelayCommand(FlushDnsAsync);
            RunSpeedTestCommand = new AsyncRelayCommand(RunSpeedTestAsync);

            InitializeDnsServers();
            _ = LoadSystemStatesAsync();
        }

        private void InitializeDnsServers()
        {
            DnsServers.Add(new DnsServerItem { Name = "Cloudflare DNS", PrimaryIp = "1.1.1.1", SecondaryIp = "1.0.0.1" });
            DnsServers.Add(new DnsServerItem { Name = "Google Public DNS", PrimaryIp = "8.8.8.8", SecondaryIp = "8.8.4.4" });
            DnsServers.Add(new DnsServerItem { Name = "Quad9 DNS", PrimaryIp = "9.9.9.9", SecondaryIp = "149.112.112.112" });
            DnsServers.Add(new DnsServerItem { Name = "OpenDNS", PrimaryIp = "208.67.222.222", SecondaryIp = "208.67.220.220" });
        }

        private async Task LoadSystemStatesAsync()
        {
            try
            {
                IsTelemetryDisabled = _performanceService.GetTelemetryState();
                IsErrorReportingDisabled = _performanceService.GetErrorReportingState();
                ActivePowerScheme = await _performanceService.GetActivePowerSchemeAsync();
                ActiveDns = await _performanceService.GetActiveDnsAsync(CancellationToken.None);

                // El Modo Juego se considera activo si el plan de energía es Alto Rendimiento
                IsGameModeEnabled = ActivePowerScheme.Contains("Alto rendimiento", StringComparison.OrdinalIgnoreCase) || 
                                    ActivePowerScheme.Contains("High performance", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error al cargar estados del sistema en PerformanceViewModel.");
            }
        }

        private async Task ToggleTelemetryAsync()
        {
            StatusMessage = "Modificando políticas de telemetría...";
            try
            {
                await _performanceService.SetTelemetryStateAsync(IsTelemetryDisabled);
                StatusMessage = IsTelemetryDisabled ? "Telemetría desactivada con éxito." : "Telemetría activada de nuevo.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task ToggleErrorReportingAsync()
        {
            StatusMessage = "Modificando directivas de informes de error...";
            try
            {
                await _performanceService.SetErrorReportingStateAsync(IsErrorReportingDisabled);
                StatusMessage = IsErrorReportingDisabled ? "Informes de error desactivados." : "Informes de error activados.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task ToggleGameModeAsync()
        {
            StatusMessage = "Alternando el Modo Juego...";
            try
            {
                bool success = await _performanceService.SetGameModeStateAsync(IsGameModeEnabled);
                if (success)
                {
                    ActivePowerScheme = await _performanceService.GetActivePowerSchemeAsync();
                    StatusMessage = IsGameModeEnabled ? "Modo Juego activado: Rendimiento Máximo configurado." : "Modo Juego desactivado. Plan Equilibrado restaurado.";
                }
                else
                {
                    StatusMessage = "No se pudo cambiar el plan de energía (requiere permisos).";
                    // Revertir switch
                    IsGameModeEnabled = !IsGameModeEnabled;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                IsGameModeEnabled = !IsGameModeEnabled;
            }
        }

        private async Task RunDnsTestAsync()
        {
            if (IsLoadingDns) return;

            IsLoadingDns = true;
            StatusMessage = "Haciendo test de velocidad en servidores DNS...";

            try
            {
                foreach (var server in DnsServers)
                {
                    server.Status = "Probando ping...";
                    server.LatencyMs = null;

                    var time = await _performanceService.PingDnsServerAsync(server.PrimaryIp, CancellationToken.None);
                    if (time.HasValue)
                    {
                        server.LatencyMs = time.Value;
                        server.Status = "Correcto";
                    }
                    else
                    {
                        server.LatencyMs = 999;
                        server.Status = "Tiempo de espera agotado";
                    }
                }

                // Ordenar la colección por menor latencia
                var sorted = DnsServers.OrderBy(x => x.LatencyMs ?? 999).ToList();
                DnsServers.Clear();
                foreach (var item in sorted)
                {
                    DnsServers.Add(item);
                }

                // Comprobar cuál está activo
                string currentDns = await _performanceService.GetActiveDnsAsync(CancellationToken.None);
                ActiveDns = currentDns;

                foreach (var server in DnsServers)
                {
                    server.IsActive = currentDns.Contains(server.PrimaryIp);
                }

                StatusMessage = "Test de velocidad DNS completado.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al ejecutar test: {ex.Message}";
            }
            finally
            {
                IsLoadingDns = false;
            }
        }

        private async Task ApplyDnsAsync(DnsServerItem? server)
        {
            if (server == null) return;

            var result = MessageBox.Show($"¿Desea configurar '{server.Name}' ({server.PrimaryIp} / {server.SecondaryIp}) como su servidor DNS de red primario?",
                                         "Confirmar Cambios de Red", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            StatusMessage = $"Aplicando DNS {server.Name}...";
            try
            {
                bool success = await _performanceService.ApplyDnsSettingsAsync(server.PrimaryIp, server.SecondaryIp, CancellationToken.None);
                if (success)
                {
                    ActiveDns = await _performanceService.GetActiveDnsAsync(CancellationToken.None);
                    
                    // Actualizar flags de activo
                    foreach (var s in DnsServers)
                    {
                        s.IsActive = (s.PrimaryIp == server.PrimaryIp);
                    }

                    StatusMessage = $"DNS cambiadas correctamente a {server.Name}.";
                    MessageBox.Show($"Se han aplicado las direcciones DNS de {server.Name} a sus adaptadores de red activos.", 
                                    "DNS Configurado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = "No se pudieron aplicar las DNS (requiere permisos de Administrador).";
                    MessageBox.Show("No se pudieron cambiar las DNS de red. Asegúrese de ejecutar la aplicación como Administrador.", 
                                    "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task ApplyFastestDnsAsync()
        {
            // Si el test de latencia no se ha realizado (todos los LatencyMs son null), lo ejecutamos primero
            if (DnsServers.All(s => !s.LatencyMs.HasValue))
            {
                await RunDnsTestAsync();
            }

            // Filtrar y ordenar para encontrar el de menor latencia (que no sea un timeout/999)
            var fastest = DnsServers
                .Where(s => s.LatencyMs.HasValue && s.LatencyMs.Value > 0 && s.LatencyMs.Value < 999)
                .OrderBy(s => s.LatencyMs ?? 999)
                .FirstOrDefault();

            if (fastest != null)
            {
                await ApplyDnsAsync(fastest);
            }
            else
            {
                MessageBox.Show("No se detectó ningún servidor DNS con latencia válida. Por favor, realice el test de velocidad DNS manualmente primero.",
                                "Sin latencia válida", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task FlushDnsAsync()
        {
            StatusMessage = "Vaciando caché DNS del sistema...";
            try
            {
                bool success = await _performanceService.FlushDnsCacheAsync(CancellationToken.None);
                StatusMessage = success ? "Caché DNS vaciada con éxito (Flush DNS)." : "No se pudo vaciar la caché DNS.";
                if (success)
                {
                    MessageBox.Show("La caché de resolución DNS de Windows ha sido vaciada con éxito.", 
                                    "Flush DNS Completado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task RunSpeedTestAsync()
        {
            if (IsSpeedTesting) return;
            IsSpeedTesting = true;
            SpeedTestStatusText = "Probando velocidad (10 MB)...";
            InternetSpeedMbps = -1;
            StatusMessage = "Ejecutando speed test de Internet con Cloudflare CDN...";
            try
            {
                double mbps = await _performanceService.RunInternetSpeedTestAsync(CancellationToken.None);
                InternetSpeedMbps = mbps;
                if (mbps < 0)
                    SpeedTestStatusText = "Error de red o sin conexión";
                else if (mbps < 5)
                    SpeedTestStatusText = $"{mbps} Mbps  ·  Muy lenta";
                else if (mbps < 25)
                    SpeedTestStatusText = $"{mbps} Mbps  ·  Aceptable";
                else if (mbps < 100)
                    SpeedTestStatusText = $"{mbps} Mbps  ·  Buena";
                else
                    SpeedTestStatusText = $"{mbps} Mbps  ·  Excelente";
                StatusMessage = $"Speed test completado: {SpeedTestStatusText}";
            }
            catch (Exception ex)
            {
                SpeedTestStatusText = "Error en el test";
                StatusMessage = $"Error en speed test: {ex.Message}";
            }
            finally
            {
                IsSpeedTesting = false;
            }
        }
    }
}
