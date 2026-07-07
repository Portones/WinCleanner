using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface IPhotosCleanupService
    {
        Task<List<PhotoItem>> GetObsoleteScreenshotsAsync(int ageInDays, CancellationToken cancellationToken);
        Task<List<DuplicatePhotoGroup>> GetDuplicatePhotosAsync(string scanPath, CancellationToken cancellationToken);
        Task<bool> DeletePhotoAsync(string filePath);
    }
}
