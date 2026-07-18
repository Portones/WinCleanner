using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class CrashInspectorService : ICrashInspectorService
    {
        public async Task<List<CrashItem>> GetRecentCrashesAsync(int maxItems, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var crashes = new List<CrashItem>();
                string[] logNames = { "Application", "System" };

                foreach (var logName in logNames)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        if (!EventLog.Exists(logName)) continue;

                        using var log = new EventLog(logName);
                        int count = log.Entries.Count;
                        int checkedItems = 0;

                        for (int i = count - 1; i >= 0 && crashes.Count < maxItems && checkedItems < 2000; i--)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            checkedItems++;

                            try
                            {
                                var entry = log.Entries[i];

                                if (IsRelevantCrashEvent(entry))
                                {
                                    crashes.Add(new CrashItem
                                    {
                                        Timestamp = entry.TimeGenerated,
                                        Source = $"{logName}: {entry.Source}",
                                        Level = entry.EntryType.ToString(),
                                        EventId = (int)(entry.InstanceId & 0xFFFF),
                                        Description = SimplifyDescription(entry.Source, entry.Message),
                                        RawMessage = entry.Message
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Verbose(ex, "Error al leer entrada {Index} de {LogName}", i, logName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error al acceder a la bitácora de eventos {LogName}", logName);
                    }
                }

                return crashes.OrderByDescending(c => c.Timestamp).Take(maxItems).ToList();
            }, cancellationToken);
        }

        private bool IsRelevantCrashEvent(EventLogEntry entry)
        {
            if (entry.EntryType == EventLogEntryType.Error) return true;

            int eventId = (int)(entry.InstanceId & 0xFFFF);
            string src = entry.Source ?? string.Empty;

            if (eventId == 41 || eventId == 1000 || eventId == 1001 || eventId == 6008) return true;
            if (src.Contains("Application Error", StringComparison.OrdinalIgnoreCase) ||
                src.Contains("Windows Error Reporting", StringComparison.OrdinalIgnoreCase) ||
                src.Contains("BugCheck", StringComparison.OrdinalIgnoreCase) ||
                src.Contains("Kernel-Power", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private string SimplifyDescription(string source, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "Error registrado sin mensaje detallado.";

            var lines = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 0)
            {
                var firstLine = lines[0].Trim();
                if (firstLine.Length > 120) return firstLine.Substring(0, 117) + "...";
                return firstLine;
            }

            return message.Length > 120 ? message.Substring(0, 117) + "..." : message;
        }
    }
}
