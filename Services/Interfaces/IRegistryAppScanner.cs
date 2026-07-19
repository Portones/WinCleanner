using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface IRegistryAppScanner
    {
        Task<List<InstalledApp>> ScanInstalledAppsAsync(CancellationToken cancellationToken);
    }
}
