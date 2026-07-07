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

            // Agrupar elementos por ID de módulo
            var itemsByModule = items.GroupBy(x => x.ModuleId).ToList();
            int totalCleaned = 0;
            int processedGroups = 0;

            foreach (var group in itemsByModule)
            {
                var module = Modules.FirstOrDefault(m => m.Id == group.Key);
                if (module != null)
                {
                    try
                    {
                        Log.Information("Iniciando limpieza en módulo: {ModuleName} ({Count} elementos)", module.Name, group.Count());
                        
                        var groupProgress = new Progress<double>(val =>
                        {
                            double baseProgress = (double)processedGroups / itemsByModule.Count * 100;
                            double currentProgress = baseProgress + (val / itemsByModule.Count);
                            progress.Report(currentProgress);
                        });

                        var cleanedCount = await module.CleanAsync(group.ToList(), groupProgress, cancellationToken);
                        totalCleaned += cleanedCount;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error al limpiar elementos del módulo {ModuleId}", group.Key);
                    }
                }
                processedGroups++;
                progress.Report((double)processedGroups / itemsByModule.Count * 100);
            }

            Log.Information("Limpieza completada. Total elementos eliminados/procesados: {TotalCleaned}", totalCleaned);
            progress.Report(100);
            return totalCleaned;
        }
    }
}
