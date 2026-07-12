using System.Collections.Generic;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface IDriverService
    {
        Task<List<DriverItem>> GetInstalledDriversAsync();
        Task<List<DriverUpdateItem>> GetAvailableDriverUpdatesAsync();
    }
}
