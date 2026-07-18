using System.Threading.Tasks;

namespace WinCleaner.Services.Interfaces
{
    public interface INetworkDiagnosticService
    {
        Task<long> MeasureLatencyAsync();
        Task<bool> FlushDnsAsync();
    }
}
