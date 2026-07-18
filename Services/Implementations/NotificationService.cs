using System;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private Action<string, string, NotificationType>? _showNotificationHandler;

        public void RegisterNotificationHandler(Action<string, string, NotificationType> handler)
        {
            _showNotificationHandler = handler;
        }

        public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
        {
            try
            {
                if (_showNotificationHandler != null)
                {
                    _showNotificationHandler.Invoke(title, message, type);
                }
                else
                {
                    Log.Information("[NOTIFICACIÓN] {Title}: {Message}", title, message);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al mostrar la notificación del sistema.");
            }
        }
    }
}
