using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class SystemTrayService : ISystemTrayService
    {
        private readonly IConfigurationService _configurationService;
        private readonly NotificationService _notificationService;
        private readonly IRamBoosterService _memoryOptimizer;
        private readonly IReportGeneratorService _reportGenerator;
        private readonly ISystemDiagnosticService _diagnosticService;
        private readonly ITemperatureService _temperatureService;

        private NotifyIcon? _notifyIcon;
        private Window? _mainWindow;
        private DispatcherTimer? _monitorTimer;

        private bool _hasWarnedHighRam;
        private bool _hasWarnedLowDisk;
        private bool _hasWarnedHighTemp;

        public SystemTrayService(
            IConfigurationService configurationService,
            INotificationService notificationService,
            IRamBoosterService memoryOptimizer,
            IReportGeneratorService reportGenerator,
            ISystemDiagnosticService diagnosticService,
            ITemperatureService temperatureService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _notificationService = (NotificationService)notificationService;
            _memoryOptimizer = memoryOptimizer ?? throw new ArgumentNullException(nameof(memoryOptimizer));
            _reportGenerator = reportGenerator ?? throw new ArgumentNullException(nameof(reportGenerator));
            _diagnosticService = diagnosticService ?? throw new ArgumentNullException(nameof(diagnosticService));
            _temperatureService = temperatureService ?? throw new ArgumentNullException(nameof(temperatureService));
        }

        public void Initialize(Window mainWindow)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));

            // Conectar el handler de notificaciones
            _notificationService.RegisterNotificationHandler(ShowBalloonTip);

            // Crear el icono de bandeja
            _notifyIcon = new NotifyIcon
            {
                Text = "WinCleaner - Diagnóstico y Optimización",
                Visible = true
            };

            // Cargar icono desde la app
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "WinCleanerLogo.ico");
                if (File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new Icon(iconPath);
                }
                else
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }

            // Crear menú contextual
            var contextMenu = new ContextMenuStrip();

            var openItem = new ToolStripMenuItem("Abrir WinCleaner");
            openItem.Font = new Font(openItem.Font, System.Drawing.FontStyle.Bold);
            openItem.Click += (s, e) => RestoreFromTray();

            var ramItem = new ToolStripMenuItem("⚡ Optimizar RAM ahora");
            ramItem.Click += async (s, e) =>
            {
                try
                {
                    long bytesFreed = await _memoryOptimizer.OptimizeRamAsync(new Progress<double>(), System.Threading.CancellationToken.None);
                    string freedText = Models.CleanableItem.FormatSize(bytesFreed);
                    ShowBalloonTip("Memoria Optimizada", $"Se han liberado {freedText} de memoria RAM.", NotificationType.Info);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al optimizar RAM desde la bandeja.");
                }
            };

            var reportItem = new ToolStripMenuItem("📊 Generar Reporte de Diagnóstico");
            reportItem.Click += async (s, e) =>
            {
                try
                {
                    string path = await _reportGenerator.GenerateAndOpenReportAsync();
                    ShowBalloonTip("Reporte Generado", $"Se ha exportado el informe de diagnóstico en su navegador.", NotificationType.Info);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al generar reporte desde la bandeja.");
                }
            };

            var exitItem = new ToolStripMenuItem("❌ Salir");
            exitItem.Click += (s, e) =>
            {
                _notifyIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            };

            contextMenu.Items.Add(openItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(ramItem);
            contextMenu.Items.Add(reportItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => RestoreFromTray();

            // Interceptar evento de minimizado/cierre de la ventana principal
            _mainWindow.StateChanged += MainWindow_StateChanged;

            // Iniciar monitoreo silencioso en segundo plano cada 30 segundos
            _monitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _monitorTimer.Tick += async (s, e) => await PerformBackgroundCheckAsync();
            _monitorTimer.Start();
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (_mainWindow == null) return;
            var settings = _configurationService.CurrentSettings;

            if (_mainWindow.WindowState == WindowState.Minimized && settings.MinimizeToTray)
            {
                HideToTray();
            }
        }

        public void HideToTray()
        {
            if (_mainWindow == null) return;
            _mainWindow.Hide();
            ShowBalloonTip("WinCleaner Activo", "La aplicación continúa ejecutándose en segundo plano para supervisar el sistema.", NotificationType.Info);
        }

        public void RestoreFromTray()
        {
            if (_mainWindow == null) return;
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }

        public void ShowBalloonTip(string title, string message, NotificationType type)
        {
            if (_notifyIcon == null) return;

            ToolTipIcon tipIcon = type switch
            {
                NotificationType.Warning => ToolTipIcon.Warning,
                NotificationType.Error => ToolTipIcon.Error,
                _ => ToolTipIcon.Info
            };

            _notifyIcon.ShowBalloonTip(3000, title, message, tipIcon);
        }

        private async System.Threading.Tasks.Task PerformBackgroundCheckAsync()
        {
            try
            {
                var settings = _configurationService.CurrentSettings;
                if (!settings.EnableBackgroundMonitoring) return;

                // 1. Monitor de RAM elevada (> 85%)
                if (settings.NotifyHighRam)
                {
                    var metrics = _diagnosticService.GetAllMetrics();
                    if (metrics.Ram.UsagePercentage >= 85)
                    {
                        if (!_hasWarnedHighRam)
                        {
                            ShowBalloonTip("Uso Elevado de Memoria RAM", $"La memoria RAM está al {metrics.Ram.UsagePercentage:F0}%. Haz clic en el icono para optimizar.", NotificationType.Warning);
                            _hasWarnedHighRam = true;
                        }
                    }
                    else
                    {
                        _hasWarnedHighRam = false;
                    }
                }

                // 2. Monitor de Espacio Libre en Disco (< 10 GB)
                if (settings.NotifyLowDiskSpace)
                {
                    var systemDrive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && string.Equals(d.Name, @"C:\", StringComparison.OrdinalIgnoreCase));
                    if (systemDrive != null)
                    {
                        long freeGb = systemDrive.AvailableFreeSpace / (1024 * 1024 * 1024);
                        if (freeGb < 10)
                        {
                            if (!_hasWarnedLowDisk)
                            {
                                ShowBalloonTip("Poco Espacio en Disco C:", $"Quedan únicamente {freeGb} GB libres en la unidad principal.", NotificationType.Warning);
                                _hasWarnedLowDisk = true;
                            }
                        }
                        else
                        {
                            _hasWarnedLowDisk = false;
                        }
                    }
                }

                // 3. Monitor de Temperatura Crítica (> 80°C)
                if (settings.NotifyHighTemp)
                {
                    var temps = await _temperatureService.GetTemperaturesAsync();
                    var hotItem = temps.FirstOrDefault(t => t.CurrentValue >= 80);
                    if (hotItem != null)
                    {
                        if (!_hasWarnedHighTemp)
                        {
                            ShowBalloonTip("Temperatura Elevada", $"Temperatura crítica en {hotItem.ComponentName} ({hotItem.CurrentValue:F0}°C).", NotificationType.Warning);
                            _hasWarnedHighTemp = true;
                        }
                    }
                    else
                    {
                        _hasWarnedHighTemp = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Verbose(ex, "Error en el chequeo de monitoreo silencioso.");
            }
        }

        public void Dispose()
        {
            if (_monitorTimer != null)
            {
                _monitorTimer.Stop();
                _monitorTimer = null;
            }

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
    }
}
