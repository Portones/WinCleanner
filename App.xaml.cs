using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Implementations;
using WinCleaner.Services.Implementations.CleanupModules;
using WinCleaner.Services.Interfaces;
using WinCleaner.ViewModels;
using WinCleaner.Views;

namespace WinCleaner
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        private bool _isExiting;
        private bool _firstMinimizeAlertShown;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ConfigureLogging();

            Log.Information("Iniciando WinCleaner...");

            // Configurar manejadores de excepciones globales
            SetupExceptionHandling();

            try
            {
                var services = new ServiceCollection();
                ConfigureServices(services);

                ServiceProvider = services.BuildServiceProvider();

                // Comprobar modo de limpieza silenciosa
                if (e.Args.Contains("--silent-clean", StringComparer.OrdinalIgnoreCase))
                {
                    Log.Information("Iniciando en modo silencioso (--silent-clean)...");
                    _ = RunSilentCleanAsync();
                    return;
                }

                // Configurar icono en la bandeja de sistema (SysTray)
                SetupSystemTray();

                // Mostrar la ventana principal inyectada
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.Closing += MainWindow_Closing;

                if (e.Args.Contains("--minimized", StringComparer.OrdinalIgnoreCase))
                {
                    Log.Information("Iniciando minimizado en la bandeja del sistema (--minimized)...");
                    mainWindow.WindowState = WindowState.Minimized;
                    mainWindow.Hide();
                }
                else
                {
                    mainWindow.Show();
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error fatal al iniciar la aplicación.");
                System.Windows.MessageBox.Show($"Ocurrió un error crítico durante el inicio de la aplicación:\n{ex.Message}", 
                                                "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void ConfigureLogging()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logDirectory = Path.Combine(appDataPath, "WinCleaner", "Logs");

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            var logFilePath = Path.Combine(logDirectory, "cleaner_log.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, 
                              outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        private void SetupExceptionHandling()
        {
            // Excepciones en el hilo principal de WPF (UI Thread)
            DispatcherUnhandledException += (s, e) =>
            {
                Log.Fatal(e.Exception, "Excepción no controlada en el despachador de la interfaz de usuario (Dispatcher).");
                System.Windows.MessageBox.Show($"Ocurrió un error crítico en la interfaz gráfica:\n{e.Exception.Message}\n\nConsulte el archivo de registro para más detalles.", 
                                                "Error Crítico de UI", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true; // Prevenir el cierre inmediato si es recuperable
            };

            // Excepciones en cualquier hilo de fondo del AppDomain
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Log.Fatal(ex, "Excepción no controlada en el AppDomain. Finalizando proceso: {IsTerminating}", e.IsTerminating);
                    System.Windows.MessageBox.Show($"Ocurrió un error de sistema no controlado:\n{ex.Message}\n\nLa aplicación se cerrará.", 
                                                    "Error Fatal del Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            // Excepciones no observadas en tareas asíncronas
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Log.Error(e.Exception, "Excepción de tarea asíncrona no observada (TPL).");
                e.SetObserved(); // Previene la terminación del proceso
            };
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Servicios de Infraestructura y Configuración
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<ISystemDiagnosticService, SystemDiagnosticService>();
            services.AddSingleton<ICleanupManagerService, CleanupManagerService>();
            services.AddSingleton<IDuplicateFinderService, DuplicateFinderService>();
            services.AddSingleton<IStartupManagerService, StartupManagerService>();
            services.AddSingleton<IBootAnalyzerService, BootAnalyzerService>();
            services.AddSingleton<IRamBoosterService, RamBoosterService>();
            services.AddSingleton<IWindowsServicesService, WindowsServicesService>();
            services.AddSingleton<IContextMenuService, ContextMenuService>();
            services.AddSingleton<IAppUninstallerService, AppUninstallerService>();
            services.AddSingleton<IDiskAnalyzerService, DiskAnalyzerService>();
            services.AddSingleton<IPerformanceService, PerformanceService>();
            services.AddSingleton<IAppUpdaterService, AppUpdaterService>();
            services.AddSingleton<IPhotosCleanupService, PhotosCleanupService>();
            services.AddSingleton<ITemperatureService, TemperatureService>();
            services.AddSingleton<IScheduledMaintenanceService, ScheduledMaintenanceService>();
            services.AddSingleton<IBatteryService, BatteryService>();
            services.AddSingleton<IDriverService, DriverService>();

            // Registrar Módulos de Limpieza (Inyección múltiple de ICleanupModule)
            services.AddSingleton<ICleanupModule, TempFilesCleanupModule>();
            services.AddSingleton<ICleanupModule, EmptyFoldersCleanupModule>();
            services.AddSingleton<ICleanupModule, RecycleBinCleanupModule>();
            services.AddSingleton<ICleanupModule, LargeFilesCleanupModule>();
            services.AddSingleton<ICleanupModule, BrowserCacheCleanupModule>();
            services.AddSingleton<ICleanupModule, RedundantInstallersCleanupModule>();
            services.AddSingleton<ICleanupModule, UnusedDownloadsCleanupModule>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<CleanupViewModel>();
            services.AddSingleton<DuplicateFilesViewModel>();
            services.AddSingleton<StartupViewModel>();
            services.AddSingleton<ServicesViewModel>();
            services.AddSingleton<ContextMenuViewModel>();
            services.AddSingleton<UninstallerViewModel>();
            services.AddSingleton<DiskAnalyzerViewModel>();
            services.AddSingleton<PerformanceViewModel>();
            services.AddSingleton<UpdaterViewModel>();
            services.AddSingleton<PhotosCleanupViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<RamOptimizerViewModel>();
            services.AddSingleton<TemperatureViewModel>();
            services.AddSingleton<BatteryViewModel>();
            services.AddSingleton<DriverViewModel>();

            // Vistas
            services.AddSingleton<MainWindow>();
        }

        private void SetupSystemTray()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            
            // Cargar de forma dinámica el icono principal compilado en el ejecutable
            if (!string.IsNullOrEmpty(Environment.ProcessPath))
            {
                try
                {
                    _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath);
                }
                catch
                {
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Shield;
                }
            }
            else
            {
                _notifyIcon.Icon = System.Drawing.SystemIcons.Shield;
            }

            _notifyIcon.Text = "WinCleaner - Suite de Optimización";
            _notifyIcon.Visible = true;

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Optimizar RAM", null, Tray_OptimizeRam);
            contextMenu.Items.Add("Limpieza Rápida", null, Tray_QuickCleanup);
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add("Mostrar WinCleaner", null, Tray_ShowApp);
            contextMenu.Items.Add("Salir", null, Tray_Exit);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExiting)
            {
                e.Cancel = true; // Prevenir cierre
                var mainWindow = ServiceProvider.GetService<MainWindow>();
                mainWindow?.Hide(); // Ocultar ventana principal

                if (!_firstMinimizeAlertShown)
                {
                    _notifyIcon?.ShowBalloonTip(3000, 
                        "WinCleaner sigue activo", 
                        "La aplicación se ha minimizado en segundo plano para seguir protegiendo su equipo.", 
                        System.Windows.Forms.ToolTipIcon.Info);
                    _firstMinimizeAlertShown = true;
                }
            }
        }

        private void ShowMainWindow()
        {
            var mainWindow = ServiceProvider.GetService<MainWindow>();
            if (mainWindow != null)
            {
                mainWindow.Show();
                if (mainWindow.WindowState == WindowState.Minimized)
                {
                    mainWindow.WindowState = WindowState.Normal;
                }
                mainWindow.Activate();
            }
        }

        private async void Tray_OptimizeRam(object? sender, EventArgs e)
        {
            Log.Information("Iniciando optimización de RAM desde la bandeja del sistema...");
            try
            {
                var ramBooster = ServiceProvider.GetRequiredService<IRamBoosterService>();
                var progress = new Progress<double>();
                long bytesFreed = await ramBooster.OptimizeRamAsync(progress, CancellationToken.None);

                string sizeText = CleanableItem.FormatSize(bytesFreed);
                _notifyIcon?.ShowBalloonTip(3000, "Optimización de RAM", $"Se liberaron {sizeText} de memoria RAM.", System.Windows.Forms.ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al optimizar RAM desde la bandeja del sistema.");
                _notifyIcon?.ShowBalloonTip(3000, "Optimización de RAM", "Ocurrió un error al liberar memoria.", System.Windows.Forms.ToolTipIcon.Error);
            }
        }

        private async void Tray_QuickCleanup(object? sender, EventArgs e)
        {
            Log.Information("Iniciando limpieza rápida desde la bandeja del sistema...");
            try
            {
                var modules = ServiceProvider.GetServices<ICleanupModule>();
                long totalFreed = 0;

                foreach (var module in modules)
                {
                    var progress = new Progress<double>();
                    var scanResult = await module.ScanAsync(progress, CancellationToken.None);
                    long sizeToFree = scanResult.TotalSize;
                    if (sizeToFree > 0)
                    {
                        await module.CleanAsync(scanResult.Items, progress, CancellationToken.None);
                        totalFreed += sizeToFree;
                    }
                }

                string sizeText = CleanableItem.FormatSize(totalFreed);
                _notifyIcon?.ShowBalloonTip(3000, "Limpieza Rápida", $"Limpieza rápida completada. Se liberaron {sizeText}.", System.Windows.Forms.ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error durante la limpieza rápida desde la bandeja del sistema.");
                _notifyIcon?.ShowBalloonTip(3000, "Limpieza Rápida", "Ocurrió un error al realizar la limpieza.", System.Windows.Forms.ToolTipIcon.Error);
            }
        }

        private void Tray_ShowApp(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void Tray_Exit(object? sender, EventArgs e)
        {
            _isExiting = true;
            var mainWindow = ServiceProvider.GetService<MainWindow>();
            mainWindow?.Close();
            Shutdown();
        }

        private async Task RunSilentCleanAsync()
        {
            try
            {
                var cleanupManager = ServiceProvider.GetRequiredService<ICleanupManagerService>();

                Log.Information("Modo Silencioso: Iniciando escaneo de archivos temporales...");
                var scanResult = await cleanupManager.ScanAllAsync(new Progress<double>(), CancellationToken.None);

                if (scanResult.Items.Count > 0)
                {
                    Log.Information("Modo Silencioso: Se encontraron {Count} elementos para limpiar. Liberando espacio...", scanResult.Items.Count);
                    int cleanedCount = await cleanupManager.CleanItemsAsync(scanResult.Items, new Progress<double>(), CancellationToken.None);
                    Log.Information("Modo Silencioso: Limpieza finalizada. Se limpiaron {Count} de {Total} elementos ({Size} bytes liberados).", 
                                    cleanedCount, scanResult.Items.Count, scanResult.TotalSize);
                }
                else
                {
                    Log.Information("Modo Silencioso: No se encontraron elementos para limpiar.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error crítico durante la limpieza silenciosa en segundo plano.");
            }
            finally
            {
                Log.Information("Modo Silencioso: Finalizando aplicación.");
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Cerrando WinCleaner.");
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
