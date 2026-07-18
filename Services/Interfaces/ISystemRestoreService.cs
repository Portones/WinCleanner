using System.Threading.Tasks;

namespace WinCleaner.Services.Interfaces
{
    public interface ISystemRestoreService
    {
        Task<int> GetSystemRestorePointCountAsync();
        Task<bool> DeleteOldRestorePointsAsync();
    }
}
