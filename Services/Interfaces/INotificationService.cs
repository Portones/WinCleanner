using System;

namespace WinCleaner.Services.Interfaces
{
    public enum NotificationType
    {
        Info,
        Warning,
        Error
    }

    public interface INotificationService
    {
        void ShowNotification(string title, string message, NotificationType type = NotificationType.Info);
    }
}
