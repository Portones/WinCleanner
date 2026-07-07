using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface ICleanupManagerService
    {
        IEnumerable<ICleanupModule> Modules { get; }
        Task<ScanResult> ScanAllAsync(IProgress<double> progress, CancellationToken cancellationToken);
        Task<int> CleanItemsAsync(List<CleanableItem> items, IProgress<double> progress, CancellationToken cancellationToken);
    }
}
