using System;
using System.Management;
using System.Threading;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    /// <summary>
    /// Monitorea el registro de desinstalaciones de Windows mediante WMI RegistryKeyChangeEvent.
    /// Dispara UninstallDetected cuando se elimina una clave en el árbol de desinstalación.
    /// </summary>
    public class UninstallWatcherService : IUninstallWatcherService
    {
        private ManagementEventWatcher? _watcher;
        private readonly INotificationService _notificationService;
        private bool _disposed;

        public event EventHandler<string>? UninstallDetected;

        public UninstallWatcherService(INotificationService notificationService)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public void StartWatching()
        {
            if (_watcher != null) return;

            try
            {
                // WQL: detecta cambios (eliminación de clave) en la rama Uninstall del usuario actual
                var query = new WqlEventQuery(
                    "SELECT * FROM RegistryKeyChangeEvent WHERE " +
                    "Hive='HKEY_LOCAL_MACHINE' AND " +
                    @"KeyPath='SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall'");

                _watcher = new ManagementEventWatcher(new ManagementScope(@"\\.\root\default"), query)
                {
                    Options = { Timeout = ManagementOptions.InfiniteTimeout }
                };

                _watcher.EventArrived += OnUninstallKeyChanged;
                _watcher.Start();
                Log.Information("UninstallWatcherService: monitoreo de desinstalaciones activo.");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "UninstallWatcherService: no se pudo iniciar el monitoreo WMI de desinstalaciones.");
            }
        }

        public void StopWatching()
        {
            try
            {
                _watcher?.Stop();
                _watcher?.Dispose();
                _watcher = null;
            }
            catch { }
        }

        private void OnUninstallKeyChanged(object sender, EventArrivedEventArgs e)
        {
            // Evitar spam: esperamos 3 segundos antes de notificar
            Thread.Sleep(3000);

            try
            {
                const string title   = "🗑️ Desinstalación Detectada";
                const string message = "Se ha desinstalado una aplicación. WinCleaner puede limpiar los residuos que hayan quedado en el sistema.";

                _notificationService.ShowNotification(title, message, NotificationType.Info);
                UninstallDetected?.Invoke(this, message);
                Log.Information("UninstallWatcherService: desinstalación detectada, notificación enviada.");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "UninstallWatcherService: error al procesar el evento de desinstalación.");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            StopWatching();
            _disposed = true;
        }
    }
}
