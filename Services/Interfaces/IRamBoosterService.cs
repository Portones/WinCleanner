using System;
using System.Threading;
using System.Threading.Tasks;

namespace WinCleaner.Services.Interfaces
{
    public interface IRamBoosterService
    {
        /// <summary>
        /// Libera memoria RAM reduciendo el espacio de trabajo de los procesos activos.
        /// </summary>
        /// <returns>La cantidad de bytes liberados.</returns>
        Task<long> OptimizeRamAsync(IProgress<double> progress, CancellationToken cancellationToken);
    }
}
