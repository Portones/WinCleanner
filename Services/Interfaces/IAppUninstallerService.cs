using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface IAppUninstallerService
    {
        Task<List<InstalledApp>> GetInstalledAppsAsync(CancellationToken cancellationToken);
        Task<bool> UninstallAppAsync(InstalledApp app, CancellationToken cancellationToken);
        Task<List<ResidualItem>> ScanResidualsAsync(InstalledApp app, CancellationToken cancellationToken);
        Task<bool> CleanResidualsAsync(List<ResidualItem> residuals, CancellationToken cancellationToken);
    }
}
