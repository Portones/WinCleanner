using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface IWindowsServicesService
    {
        Task<List<ServiceItem>> GetServicesAsync(CancellationToken cancellationToken);
        Task<bool> StartServiceAsync(string serviceName, CancellationToken cancellationToken);
        Task<bool> StopServiceAsync(string serviceName, CancellationToken cancellationToken);
        Task<bool> ChangeServiceStartTypeAsync(string serviceName, string startType, CancellationToken cancellationToken);
    }
}
