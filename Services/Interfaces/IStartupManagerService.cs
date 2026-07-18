using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface IStartupManagerService
    {
        Task<List<StartupApp>> GetStartupAppsAsync(CancellationToken cancellationToken);
        Task<bool> ToggleStartupAppAsync(StartupApp app, bool enable, CancellationToken cancellationToken);
        void SetWindowsAutoStart(bool enable, bool minimized);
    }
}
