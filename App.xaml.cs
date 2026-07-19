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
using WinCleaner.ViewModels.Categories;
using WinCleaner.Views;

namespace WinCleaner
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Registrar proveedor de codificaciones para dar soporte a codificaciones heredadas como CodePage 850 (usada en consolas Windows)
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

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

                // Mostrar la ventana principal inyectada
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();

                // Inicializar servicio de bandeja del sistema y monitoreo silencioso
                var trayService = ServiceProvider.GetRequiredService<ISystemTrayService>();
                trayService.Initialize(mainWindow);

                // Iniciar el guardián de desinstalaciones en segundo plano
                var uninstallWatcher = ServiceProvider.GetRequiredService<IUninstallWatcherService>();
                uninstallWatcher.StartWatching();

                // Comprobar actualizaciones automáticamente al iniciar si está habilitado
                var configService = ServiceProvider.GetRequiredService<IConfigurationService>();
                if (configService.CurrentSettings.AutoCheckUpdates)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var updateService = ServiceProvider.GetRequiredService<IWinCleanerUpdateService>();
                            var updateInfo = await updateService.CheckForUpdatesAsync();
                            if (updateInfo.IsUpdateAvailable)
                            {
                                var notificationService = ServiceProvider.GetRequiredService<INotificationService>();
                                notificationService.ShowNotification(
                                    "Actualización de WinCleaner Disponible",
                                    $"La versión v{updateInfo.LatestVersion} está disponible. Ve a Configuración para actualizar.",
                                    NotificationType.Info);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Verbose(ex, "Error en la comprobación automática de actualizaciones al iniciar.");
                        }
                    });
                }

                if (e.Args.Contains("--minimized", StringComparer.OrdinalIgnoreCase))
                {
                    Log.Information("Iniciando minimizado en la bandeja del sistema (--minimized)...");
                    mainWindow.WindowState = WindowState.Minimized;
                    trayService.HideToTray();
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
            services.AddSingleton<IRegistryAppScanner, RegistryAppScanner>();
            services.AddSingleton<IAppIconProvider, AppIconProvider>();
            services.AddSingleton<IAppUninstallerService, AppUninstallerService>();
            services.AddSingleton<IDiskAnalyzerService, DiskAnalyzerService>();
            services.AddSingleton<IPerformanceService, PerformanceService>();
            services.AddSingleton<IAppUpdaterService, AppUpdaterService>();
            services.AddSingleton<IPhotosCleanupService, PhotosCleanupService>();
            services.AddSingleton<ITemperatureService, TemperatureService>();
            services.AddSingleton<IScheduledMaintenanceService, ScheduledMaintenanceService>();
            services.AddSingleton<IBatteryService, BatteryService>();
            services.AddSingleton<IDriverService, DriverService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IReportGeneratorService, ReportGeneratorService>();
            services.AddSingleton<ISystemTrayService, SystemTrayService>();
            services.AddSingleton<INetworkDiagnosticService, NetworkDiagnosticService>();
            services.AddSingleton<ISystemRestoreService, SystemRestoreService>();
            services.AddSingleton<ISmartAssistantService, SmartAssistantService>();
            services.AddSingleton<IBrowserCleanupService, BrowserCleanupService>();
            services.AddSingleton<IEventLogCleanerService, EventLogCleanerService>();
            services.AddSingleton<IUninstallWatcherService, UninstallWatcherService>();
            services.AddSingleton<IWinCleanerUpdateService, WinCleanerUpdateService>();
            services.AddSingleton<ISystemRepairService, SystemRepairService>();
            services.AddSingleton<ICrashInspectorService, CrashInspectorService>();
            services.AddSingleton<IRuntimeInstallerService, RuntimeInstallerService>();
            services.AddSingleton<ITcpTweakerService, TcpTweakerService>();
            services.AddSingleton<ISsdOptimizerService, SsdOptimizerService>();

            // Registrar Módulos de Limpieza (Inyección múltiple de ICleanupModule)
            services.AddSingleton<ICleanupModule, TempFilesCleanupModule>();
            services.AddSingleton<ICleanupModule, EmptyFoldersCleanupModule>();
            services.AddSingleton<ICleanupModule, RecycleBinCleanupModule>();
            services.AddSingleton<ICleanupModule, LargeFilesCleanupModule>();
            services.AddSingleton<ICleanupModule, BrowserCacheCleanupModule>();
            services.AddSingleton<ICleanupModule, RedundantInstallersCleanupModule>();
            services.AddSingleton<ICleanupModule, UnusedDownloadsCleanupModule>();
            services.AddSingleton<ICleanupModule, DevAndAppCacheCleanupModule>();

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
            services.AddSingleton<BrowserCleanupViewModel>();
            services.AddSingleton<SystemRepairViewModel>();
            services.AddSingleton<CrashInspectorViewModel>();
            services.AddSingleton<RuntimeInstallerViewModel>();
            services.AddSingleton<TcpTweakerViewModel>();
            services.AddSingleton<CleanupHistoryViewModel>();
            services.AddSingleton<SsdOptimizerViewModel>();

            // ViewModels de Categorías
            services.AddSingleton<DiagnosticsCategoryViewModel>();
            services.AddSingleton<CleanupCategoryViewModel>();
            services.AddSingleton<DiskCategoryViewModel>();
            services.AddSingleton<AppCategoryViewModel>();
            services.AddSingleton<OptimizationCategoryViewModel>();

            // Vistas
            services.AddSingleton<MainWindow>();
        }

        private async Task RunSilentCleanAsync()
        {
            try
            {
                var cleanupManager = ServiceProvider.GetRequiredService<ICleanupManagerService>();

                Log.Information("Modo Silencioso: Iniciando escaneo de archivos temporales...");
                var scanResult = await cleanupManager.ScanAllAsync("Todos", new Progress<double>(), CancellationToken.None);

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
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
