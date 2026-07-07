using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface IAppUpdaterService
    {
        bool IsWingetInstalled();
        Task<List<AppUpdateItem>> GetAvailableUpdatesAsync(CancellationToken cancellationToken);
        Task<bool> UpgradeAppAsync(AppUpdateItem app, CancellationToken cancellationToken);
    }
}
