using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface IRuntimeInstallerService
    {
        List<RuntimeItem> GetAvailableRuntimes();
        Task<bool> InstallRuntimeAsync(RuntimeItem item, IProgress<string> logProgress, CancellationToken cancellationToken);
    }
}
