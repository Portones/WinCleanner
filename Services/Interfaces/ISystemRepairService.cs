using System;
using System.Threading;
using System.Threading.Tasks;

namespace WinCleaner.Services.Interfaces
{
    public interface ISystemRepairService
    {
        Task RunSfcScanAsync(IProgress<string> outputProgress, IProgress<double> percentProgress, CancellationToken cancellationToken);
        Task RunDismRepairAsync(IProgress<string> outputProgress, IProgress<double> percentProgress, CancellationToken cancellationToken);
    }
}
