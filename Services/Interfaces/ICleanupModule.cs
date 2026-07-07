using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface ICleanupModule
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        Task<ScanResult> ScanAsync(IProgress<double> progress, CancellationToken cancellationToken);
        Task<int> CleanAsync(List<CleanableItem> itemsToClean, IProgress<double> progress, CancellationToken cancellationToken);
    }
}
