using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public class EventLogInfo
    {
        public string LogName      { get; set; } = string.Empty;
        public long   SizeBytes    { get; set; }
        public int    EntryCount   { get; set; }
        public bool   IsSelected   { get; set; } = true;
    }

    public interface IEventLogCleanerService
    {
        Task<List<EventLogInfo>> GetEventLogsAsync(CancellationToken cancellationToken);
        Task<int> ClearEventLogsAsync(IEnumerable<EventLogInfo> logs, IProgress<double> progress, CancellationToken cancellationToken);
    }
}
