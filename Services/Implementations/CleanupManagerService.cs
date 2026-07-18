using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class CleanupManagerService : ICleanupManagerService
    {
        public IEnumerable<ICleanupModule> Modules { get; }

        public CleanupManagerService(IEnumerable<ICleanupModule> modules)
        {
            Modules = modules ?? throw new ArgumentNullException(nameof(modules));
        }

        public async Task<ScanResult> ScanAllAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var modulesList = Modules.ToList();
            if (modulesList.Count == 0) return new ScanResult();

            Log.Information("Iniciando escaneo general de limpieza en {Count} módulos.", modulesList.Count);

            var tasks = new List<Task<ScanResult>>();
            var progressTracker = new double[modulesList.Count];
            var objLock = new object();

            for (int i = 0; i < modulesList.Count; i++)
            {
                int index = i;
                var module = modulesList[i];
                var moduleProgress = new Progress<double>(val =>
                {
                    lock (objLock)
                    {
                        progressTracker[index] = val;
                        double totalProgress = progressTracker.Sum() / modulesList.Count;
                        progress.Report(totalProgress);
                    }
                });

                tasks.Add(Task.Run(() => module.ScanAsync(moduleProgress, cancellationToken), cancellationToken));
            }

            try
            {
                var results = await Task.WhenAll(tasks);
                var mergedResult = new ScanResult();
                foreach (var result in results)
                {
                    mergedResult.Items.AddRange(result.Items);
                    mergedResult.TotalSize += result.TotalSize;
                }
                
                Log.Information("Escaneo general completado. Total recuperable: {SizeText} ({FilesCount} elementos)", 
                    CleanableItem.FormatSize(mergedResult.TotalSize), mergedResult.FilesCount);
                
                progress.Report(100);
                return mergedResult;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error durante el escaneo general de limpieza.");
                throw;
            }
        }

        public async Task<int> CleanItemsAsync(List<CleanableItem> items, IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (items == null || items.Count == 0) return 0;

            Log.Information("Iniciando proceso de limpieza para {Count} elementos.", items.Count);

            var itemsByModule = items.GroupBy(x => x.ModuleId).ToList();
            int totalCleaned = 0;
            int totalItemsCount = items.Count;
            int processedItemsCount = 0;

            foreach (var group in itemsByModule)
            {
                var module = Modules.FirstOrDefault(m => m.Id == group.Key);
                if (module != null)
                {
                    try
                    {
                        var groupList = group.ToList();
                        var groupProgress = new Progress<double>(val =>
                        {
                            double currentGroupItems = (val / 100.0) * groupList.Count;
                            double overallProgress = ((processedItemsCount + currentGroupItems) / totalItemsCount) * 100.0;
                            progress.Report(overallProgress);
                        });

                        var cleanedCount = await module.CleanAsync(groupList, groupProgress, cancellationToken);
                        totalCleaned += cleanedCount;
                        processedItemsCount += groupList.Count;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error al limpiar elementos del módulo {ModuleId}", group.Key);
                        processedItemsCount += group.Count();
                    }
                }
                else
                {
                    processedItemsCount += group.Count();
                }

                progress.Report((double)processedItemsCount / totalItemsCount * 100.0);
            }

            Log.Information("Limpieza completada. Total elementos eliminados/procesados: {TotalCleaned}", totalCleaned);
            progress.Report(100);
            return totalCleaned;
        }
    }
}
