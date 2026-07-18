using System;
using System.Threading;
using System.Threading.Tasks;

namespace WinCleaner.Services.Interfaces
{
    public class WinCleanerUpdateInfo
    {
        public bool IsUpdateAvailable { get; set; }
        public string LatestVersion { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime PublishedAt { get; set; }
    }

    public interface IWinCleanerUpdateService
    {
        Task<WinCleanerUpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
        Task<bool> DownloadAndInstallUpdateAsync(string downloadUrl, IProgress<double> progress, CancellationToken cancellationToken = default);
    }
}
