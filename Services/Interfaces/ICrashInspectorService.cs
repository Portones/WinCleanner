using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface ICrashInspectorService
    {
        Task<List<CrashItem>> GetRecentCrashesAsync(int maxItems, CancellationToken cancellationToken);
    }
}
