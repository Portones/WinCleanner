using System.Threading.Tasks;

namespace WinCleaner.Services.Interfaces
{
    public interface ITcpTweakerService
    {
        Task<bool> IsTcpAutoTuningEnabledAsync();
        Task<bool> SetTcpAutoTuningAsync(bool enable);
        Task<bool> IsNagleAlgorithmDisabledAsync();
        Task<bool> SetNagleAlgorithmDisabledAsync(bool disable);
        Task<bool> IsTcpChimneyOffloadEnabledAsync();
        Task<bool> SetTcpChimneyOffloadAsync(bool enable);
    }
}
