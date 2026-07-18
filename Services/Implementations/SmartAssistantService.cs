using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class SmartAssistantService : ISmartAssistantService
    {
        private readonly INetworkDiagnosticService _networkService;
        private readonly ISystemRestoreService _restoreService;

        public SmartAssistantService(
            INetworkDiagnosticService networkService,
            ISystemRestoreService restoreService)
        {
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
            _restoreService = restoreService ?? throw new ArgumentNullException(nameof(restoreService));
        }

        public async Task<List<OptimizationRecommendation>> GetSmartRecommendationsAsync()
        {
            return await Task.Run(async () =>
            {
                var list = new List<OptimizationRecommendation>();

                try
                {
                    // 1. Diagnóstico de Red y Latencia (Flush DNS)
                    long latency = await _networkService.MeasureLatencyAsync();
                    if (latency > 100)
                    {
                        list.Add(new OptimizationRecommendation
                        {
                            Text = $"Latencia elevada de red detectada ({latency} ms). Vaciar la caché DNS puede restaurar la fluidez.",
                            ActionText = "Vaciar Caché DNS",
                            ActionType = "FlushDns",
                            ColorHex = "#38BDF8",
                            IconPathData = "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.95-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v1.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z"
                        });
                    }

                    // 2. Archivos antiguos en Descargas (> 30 días y > 100 MB)
                    string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    if (Directory.Exists(downloadsPath))
                    {
                        var oldFiles = Directory.GetFiles(downloadsPath)
                            .Select(f => new FileInfo(f))
                            .Where(fi => (DateTime.Now - fi.LastAccessTime).TotalDays > 30 && fi.Length > 10 * 1024 * 1024)
                            .ToList();

                        if (oldFiles.Count > 0)
                        {
                            long totalSize = oldFiles.Sum(f => f.Length);
                            string sizeText = CleanableItem.FormatSize(totalSize);
                            list.Add(new OptimizationRecommendation
                            {
                                Text = $"Se encontraron {oldFiles.Count} archivos antiguos en Descargas sin usar ({sizeText}).",
                                ActionText = "Revisar Descargas",
                                ActionType = "CleanOldDownloads",
                                ColorHex = "#F59E0B",
                                IconPathData = "M19 9h-4V3H9v6H5l7 7 7-7zM5 18v2h14v-2H5z"
                            });
                        }
                    }

                    // 3. Capturas de pantalla acumuladas (Pictures/Screenshots)
                    string screenshotsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
                    if (Directory.Exists(screenshotsPath))
                    {
                        var shots = Directory.GetFiles(screenshotsPath, "*.png").Length;
                        if (shots > 15)
                        {
                            list.Add(new OptimizationRecommendation
                            {
                                Text = $"Hay {shots} capturas de pantalla acumuladas en Imágenes/Screenshots.",
                                ActionText = "Ver Capturas",
                                ActionType = "CleanScreenshots",
                                ColorHex = "#EC4899",
                                IconPathData = "M21 19V5c0-1.1-.9-2-2-2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2zM8.5 13.5l2.5 3.01L14.5 12l4.5 6H5l3.5-4.5z"
                            });
                        }
                    }

                    // 4. Puntos de Restauración Antiguos
                    int restoreCount = await _restoreService.GetSystemRestorePointCountAsync();
                    if (restoreCount > 2)
                    {
                        list.Add(new OptimizationRecommendation
                        {
                            Text = $"Existen {restoreCount} Puntos de Restauración acumulados. Puede liberar espacio conservando el más reciente.",
                            ActionText = "Limpiar Restauración",
                            ActionType = "CleanRestorePoints",
                            ColorHex = "#818CF8",
                            IconPathData = "M13 3c-4.97 0-9 4.03-9 9H1l3.89 3.89.07.14L9 12H6c0-3.87 3.13-7 7-7s7 3.13 7 7-3.13 7-7 7c-1.93 0-3.68-.79-4.94-2.06l-1.42 1.42C8.27 19.99 10.51 21 13 21c4.97 0 9-4.03 9-9s-4.03-9-9-9z"
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al generar recomendaciones del asistente inteligente.");
                }

                return list;
            });
        }
    }
}
