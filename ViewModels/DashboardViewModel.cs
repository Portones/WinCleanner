using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IConfigurationService _configurationService;
        private readonly ISystemDiagnosticService _diagnosticService;
        private readonly IRamBoosterService _ramBooster;
        private readonly DispatcherTimer _timer;

        private string _statusMessage = "WinCleaner - Diagnóstico en Tiempo Real Activo";
        private double _cpuUsage;
        private string _cpuTemp = "No disponible";
        private RamMetrics _ram = new();
        private List<DiskMetrics> _disks = new();
        private List<GpuMetrics> _gpus = new();
        private string _overallHealth = "Calculando...";
        private string _healthColor = "#10B981";
        private string _uptime = "Cargando...";
        private string _lastScanTimeText = "Hace poco";
        private DateTime _lastScanTime = DateTime.Now;
        private bool _isOptimizingRam;
        private List<OptimizationRecommendation> _recommendations = new();

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public double CpuUsage
        {
            get => _cpuUsage;
            set => SetProperty(ref _cpuUsage, value);
        }

        public string CpuTemp
        {
            get => _cpuTemp;
            set => SetProperty(ref _cpuTemp, value);
        }

        public RamMetrics Ram
        {
            get => _ram;
            set => SetProperty(ref _ram, value);
        }

        public List<DiskMetrics> Disks
        {
            get => _disks;
            set => SetProperty(ref _disks, value);
        }

        public List<GpuMetrics> Gpus
        {
            get => _gpus;
            set => SetProperty(ref _gpus, value);
        }

        public string OverallHealth
        {
            get => _overallHealth;
            set => SetProperty(ref _overallHealth, value);
        }

        public string HealthColor
        {
            get => _healthColor;
            set => SetProperty(ref _healthColor, value);
        }

        public string Uptime
        {
            get => _uptime;
            set => SetProperty(ref _uptime, value);
        }

        public string LastScanTimeText
        {
            get => _lastScanTimeText;
            set => SetProperty(ref _lastScanTimeText, value);
        }

        public bool IsOptimizingRam
        {
            get => _isOptimizingRam;
            set
            {
                if (SetProperty(ref _isOptimizingRam, value))
                {
                    OnPropertyChanged(nameof(CanOptimizeRam));
                }
            }
        }

        public bool CanOptimizeRam => !IsOptimizingRam;

        public List<OptimizationRecommendation> Recommendations
        {
            get => _recommendations;
            set => SetProperty(ref _recommendations, value);
        }

        public ICommand OptimizeRamCommand { get; }
        public ICommand ExecuteRecommendationCommand { get; }

        public DashboardViewModel(
            IConfigurationService configurationService, 
            ISystemDiagnosticService diagnosticService,
            IRamBoosterService ramBooster)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _diagnosticService = diagnosticService ?? throw new ArgumentNullException(nameof(diagnosticService));
            _ramBooster = ramBooster ?? throw new ArgumentNullException(nameof(ramBooster));

            OptimizeRamCommand = new AsyncRelayCommand(OptimizeRamAsync);
            ExecuteRecommendationCommand = new AsyncRelayCommand<OptimizationRecommendation>(ExecuteRecommendationAsync);

            RefreshMetrics();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!IsOptimizingRam)
            {
                RefreshMetrics();
            }
        }

        private void RefreshMetrics()
        {
            try
            {
                var allMetrics = _diagnosticService.GetAllMetrics();
                CpuUsage = allMetrics.Cpu.UsagePercentage;
                CpuTemp = allMetrics.Cpu.TemperatureText;
                Ram = allMetrics.Ram;
                Disks = allMetrics.Disks;
                Gpus = allMetrics.Gpus;
                OverallHealth = allMetrics.OverallHealthStatus;
                Uptime = allMetrics.UptimeText;

                if (OverallHealth == "Excelente") HealthColor = "#10B981";
                else if (OverallHealth == "Bueno") HealthColor = "#3B82F6";
                else if (OverallHealth == "Requiere Optimización") HealthColor = "#F59E0B";
                else HealthColor = "#EF4444";

                var elapsed = DateTime.Now - _lastScanTime;
                if (elapsed.TotalMinutes < 1)
                {
                    LastScanTimeText = "Hace menos de un minuto";
                }
                else
                {
                    LastScanTimeText = $"Hace {elapsed.Minutes} min";
                }

                // Calcular recomendaciones en tiempo real
                GenerateRecommendations();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error al refrescar las métricas en el DashboardViewModel.");
            }
        }

        private void GenerateRecommendations()
        {
            var recs = new List<OptimizationRecommendation>();

            // 1. Alerta por tiempo encendido (Uptime) > 3 días
            var uptime = _diagnosticService.GetUptime();
            if (uptime.TotalDays >= 3)
            {
                recs.Add(new OptimizationRecommendation
                {
                    Text = $"El equipo lleva encendido {uptime.Days} días. Reinicie para liberar caché y memoria retenida.",
                    ActionText = "Reiniciar PC",
                    ActionType = "Reboot",
                    ColorHex = "#EF4444", // Rojo (Advertencia crítica de Uptime)
                    IconPathData = "M12,4V1L8,5l4,4V6c3.31,0,6,2.69,6,6c0,1.01-.25,1.97-.7,2.8l1.46,1.46C19.54,14.95,20,13.54,20,12C20,7.58,16.42,4,12,4z M12,18c-3.31,0-6-2.69-6-6c0-1.01,.25-1.97,.7-2.8L5.24,7.74C4.46,9.05,4,10.46,4,12c0,4.42,3.58,8,8,8v3l4-4l-4-4v3z"
                });
            }

            // 2. Alerta por reinicio pendiente de Windows Update o Renombrado de archivos
            if (_diagnosticService.IsRebootRequired())
            {
                recs.Add(new OptimizationRecommendation
                {
                    Text = "Hay actualizaciones o cambios pendientes que requieren reiniciar el equipo.",
                    ActionText = "Reiniciar PC",
                    ActionType = "Reboot",
                    ColorHex = "#3B82F6", // Azul (Windows Update)
                    IconPathData = "M12,4V1L8,5l4,4V6c3.31,0,6,2.69,6,6c0,1.01-.25,1.97-.7,2.8l1.46,1.46C19.54,14.95,20,13.54,20,12C20,7.58,16.42,4,12,4z"
                });
            }

            // 3. Alerta por consumo de memoria RAM > 80%
            if (Ram.UsagePercentage > 80)
            {
                recs.Add(new OptimizationRecommendation
                {
                    Text = $"La memoria RAM está saturada ({Ram.UsagePercentage}%). Libere memoria inactiva.",
                    ActionText = "Optimizar RAM",
                    ActionType = "OptimizeRam",
                    ColorHex = "#F59E0B", // Ámbar/Naranja
                    IconPathData = "M21 16V8a2 2 0 0 0-2-2h-2V3a1 1 0 0 0-1-1h-2a1 1 0 0 0-1 1v3h-2V3a1 1 0 0 0-1-1H8a1 1 0 0 0-1 1v3H5a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h2v3a1 1 0 0 0 1 1h2a1 1 0 0 0 1-1v-3h2v3a1 1 0 0 0 1 1h2a1 1 0 0 0 1-1v-3h2a2 2 0 0 0 2-2z"
                });
            }

            // 4. Alerta por almacenamiento de disco casi lleno (espacio libre < 10 GB en cualquier disco)
            if (Disks != null)
            {
                foreach (var disk in Disks)
                {
                    if (disk.FreeSpaceGb < 10)
                    {
                        recs.Add(new OptimizationRecommendation
                        {
                            Text = $"Espacio crítico en disco {disk.DriveLetter} (Menos de 10 GB libres). Realice una limpieza.",
                            ActionText = "Limpiar Disco",
                            ActionType = "NavigateCleanup",
                            ColorHex = "#EF4444", // Rojo
                            IconPathData = "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14zM7 17h2v-3H7v3zm4 0h2V7h-2v10zm4 0h2v-6h-2v6z"
                        });
                        break; // Mostrar solo una alerta de disco para no saturar
                    }
                }
            }

            Recommendations = recs;
        }

        private async Task ExecuteRecommendationAsync(OptimizationRecommendation? rec)
        {
            if (rec == null) return;

            if (rec.ActionType == "Reboot")
            {
                var result = MessageBox.Show("¿Está seguro de que desea reiniciar su equipo ahora?\n\nGuarde cualquier archivo y trabajo abierto antes de proceder.", 
                                             "Confirmar Reinicio del Sistema", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        StatusMessage = "Reiniciando el sistema operativo...";
                        Process.Start("shutdown.exe", "/r /t 0");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"No se pudo iniciar el proceso de reinicio.\nDetalle: {ex.Message}", "Error de Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else if (rec.ActionType == "OptimizeRam")
            {
                await OptimizeRamAsync();
            }
            else if (rec.ActionType == "NavigateCleanup")
            {
                var mainVm = Application.Current.MainWindow?.DataContext as MainViewModel;
                mainVm?.NavigateCommand.Execute("Cleanup");
            }
        }

        private async Task OptimizeRamAsync()
        {
            if (IsOptimizingRam) return;

            IsOptimizingRam = true;
            StatusMessage = "Optimizando memoria RAM del sistema...";

            try
            {
                var progress = new Progress<double>();
                var token = CancellationToken.None;

                long bytesFreed = await _ramBooster.OptimizeRamAsync(progress, token);

                RefreshMetrics();
                _lastScanTime = DateTime.Now;

                string sizeFreedText = CleanableItem.FormatSize(bytesFreed);
                StatusMessage = $"Optimización completada. Se liberaron {sizeFreedText} de RAM.";

                MessageBox.Show($"La optimización de RAM ha finalizado con éxito.\nSe liberaron {sizeFreedText} de memoria física.", 
                                "Optimización de RAM", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = "Error al optimizar la memoria RAM.";
                Serilog.Log.Error(ex, "Error al optimizar RAM en DashboardViewModel.");
            }
            finally
            {
                IsOptimizingRam = false;
            }
        }

        public void StopTimer()
        {
            _timer.Stop();
        }
    }
}
