using System.Collections.Generic;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface IBatteryService
    {
        Task<BatteryInfo> GetBatteryInfoAsync();
        Task<List<PowerPlanItem>> GetPowerPlansAsync();
        Task<bool> SetActivePowerPlanAsync(string planGuid);
    }
}
