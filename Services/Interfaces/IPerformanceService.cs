using System.Threading;
using System.Threading.Tasks;

namespace WinCleaner.Services.Interfaces
{
    public interface IPerformanceService
    {
        Task<long?> PingDnsServerAsync(string ipAddress, CancellationToken cancellationToken);
        Task<bool> ApplyDnsSettingsAsync(string primaryDns, string secondaryDns, CancellationToken cancellationToken);
        Task<bool> FlushDnsCacheAsync(CancellationToken cancellationToken);
        Task<string> GetActiveDnsAsync(CancellationToken cancellationToken);
        
        bool GetTelemetryState();
        Task SetTelemetryStateAsync(bool disableTelemetry);
        
        bool GetErrorReportingState();
        Task SetErrorReportingStateAsync(bool disableErrorReporting);
        
        bool GetAdvertisingIdState();
        Task SetAdvertisingIdStateAsync(bool disableAdvertisingId);

        bool GetCortanaState();
        Task SetCortanaStateAsync(bool disableCortana);

        bool GetSharedExperiencesState();
        Task SetSharedExperiencesStateAsync(bool disableSharedExperiences);

        Task<bool> SetGameModeStateAsync(bool enableGameMode);
        Task<string> GetActivePowerSchemeAsync();

        /// <summary>Descarga un fragmento de datos de un CDN y mide la velocidad en Mbps.</summary>
        Task<double> RunInternetSpeedTestAsync(CancellationToken cancellationToken);
    }
}
