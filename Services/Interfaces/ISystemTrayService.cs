using System;
using System.Windows;

namespace WinCleaner.Services.Interfaces
{
    public interface ISystemTrayService : IDisposable
    {
        void Initialize(Window mainWindow);
        void ShowBalloonTip(string title, string message, NotificationType type);
        void HideToTray();
        void RestoreFromTray();
    }
}
