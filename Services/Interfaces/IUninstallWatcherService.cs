using System;

namespace WinCleaner.Services.Interfaces
{
    public interface IUninstallWatcherService : IDisposable
    {
        void StartWatching();
        void StopWatching();
        event EventHandler<string> UninstallDetected;
    }
}
