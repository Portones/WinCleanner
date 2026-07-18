using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface ISsdOptimizerService
    {
        Task<List<DriveMediumInfo>> GetStorageDrivesAsync();
        Task<bool> OptimizeDriveAsync(DriveMediumInfo drive, IProgress<string> outputProgress, CancellationToken cancellationToken);
    }
}
