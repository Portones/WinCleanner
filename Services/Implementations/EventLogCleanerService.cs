using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class EventLogCleanerService : IEventLogCleanerService
    {
        private static readonly string[] _targetLogs =
        {
            "Application", "System", "Security",
            "Setup", "Microsoft-Windows-WindowsUpdateClient/Operational",
            "Microsoft-Windows-Defender/Operational",
            "Microsoft-Windows-TaskScheduler/Operational"
        };

        public async Task<List<EventLogInfo>> GetEventLogsAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var list = new List<EventLogInfo>();

                foreach (var logName in _targetLogs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        using var session = new EventLogSession();
                        var config = new EventLogConfiguration(logName, session);
                        long sizeBytes = 0;
                        int entryCount = 0;

                        try
                        {
                            // Obtener tamaño del log del archivo físico
                            if (!string.IsNullOrEmpty(config.LogFilePath))
                            {
                                var fi = new System.IO.FileInfo(
                                    System.Environment.ExpandEnvironmentVariables(config.LogFilePath));
                                if (fi.Exists) sizeBytes = fi.Length;
                            }
                        }
                        catch { }

                        try
                        {
                            using var query = new EventLogReader(new EventLogQuery(logName, PathType.LogName));
                            while (query.ReadEvent() != null)
                            {
                                entryCount++;
                                if (entryCount >= 10000) break; // límite de conteo rápido
                            }
                        }
                        catch { }

                        list.Add(new EventLogInfo
                        {
                            LogName    = logName,
                            SizeBytes  = sizeBytes,
                            EntryCount = entryCount,
                            IsSelected = true
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Verbose(ex, "No se pudo acceder al registro de eventos {Log}", logName);
                    }
                }

                return list;
            }, cancellationToken);
        }

        public async Task<int> ClearEventLogsAsync(
            IEnumerable<EventLogInfo> logs, IProgress<double> progress, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                int cleared = 0;
                var list = logs.ToList();

                for (int i = 0; i < list.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var logInfo = list[i];

                    try
                    {
                        using var session = new EventLogSession();
                        session.ClearLog(logInfo.LogName);
                        cleared++;
                        Log.Information("Registro de eventos '{Log}' vaciado correctamente.", logInfo.LogName);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Log.Warning("Sin permisos para vaciar el registro '{Log}'. Se requiere elevación.", logInfo.LogName);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error al vaciar el registro '{Log}'.", logInfo.LogName);
                    }

                    progress.Report((double)(i + 1) / list.Count * 100);
                }

                return cleared;
            }, cancellationToken);
        }
    }
}
