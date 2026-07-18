using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public class BrowserProfile
    {
        public string BrowserName { get; set; } = string.Empty;
        public string ProfilePath  { get; set; } = string.Empty;
        public long   CacheSizeBytes { get; set; }
    }

    public interface IBrowserCleanupService
    {
        Task<List<BrowserProfile>> ScanBrowserCacheAsync(IProgress<string> progress, CancellationToken cancellationToken);
        Task<long> CleanBrowserCacheAsync(IEnumerable<BrowserProfile> profiles, IProgress<double> progress, CancellationToken cancellationToken);
    }
}
