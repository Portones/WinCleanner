using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class ReportGeneratorService : IReportGeneratorService
    {
        private readonly ISystemDiagnosticService _diagnosticService;
        private readonly ITemperatureService _temperatureService;
        private readonly IStartupManagerService _startupService;
        private readonly IConfigurationService _configurationService;

        public ReportGeneratorService(
            ISystemDiagnosticService diagnosticService,
            ITemperatureService temperatureService,
            IStartupManagerService startupService,
            IConfigurationService configurationService)
        {
            _diagnosticService = diagnosticService ?? throw new ArgumentNullException(nameof(diagnosticService));
            _temperatureService = temperatureService ?? throw new ArgumentNullException(nameof(temperatureService));
            _startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        public async Task<string> GenerateAndOpenReportAsync()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var metrics = _diagnosticService.GetAllMetrics();
                    var temps = await _temperatureService.GetTemperaturesAsync();
                    var startupApps = await _startupService.GetStartupAppsAsync(System.Threading.CancellationToken.None);
                    var settings = _configurationService.CurrentSettings;

                    long totalCleanedBytes = settings.CleanupHistory.Sum(h => h.BytesCleaned);
                    string totalCleanedText = Models.CleanableItem.FormatSize(totalCleanedBytes);

                    string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WinCleaner_Reportes");
                    Directory.CreateDirectory(folderPath);

                    string fileName = $"Reporte_WinCleaner_{DateTime.Now:yyyy-MM-dd_HHmmss}.html";
                    string filePath = Path.Combine(folderPath, fileName);

                    var sb = new StringBuilder();
                    sb.AppendLine("<!DOCTYPE html>");
                    sb.AppendLine("<html lang='es'>");
                    sb.AppendLine("<head>");
                    sb.AppendLine("  <meta charset='UTF-8'>");
                    sb.AppendLine("  <title>Informe de Diagnóstico - WinCleaner</title>");
                    sb.AppendLine("  <style>");
                    sb.AppendLine("    body { font-family: 'Segoe UI', Roboto, sans-serif; background-color: #0F172A; color: #F8FAFC; margin: 0; padding: 30px; }");
                    sb.AppendLine("    .container { max-width: 900px; margin: 0 auto; background-color: #1E293B; border-radius: 12px; padding: 30px; border: 1px solid #334155; box-shadow: 0 10px 25px rgba(0,0,0,0.5); }");
                    sb.AppendLine("    .header { border-bottom: 2px solid #334155; padding-bottom: 20px; margin-bottom: 25px; display: flex; justify-content: space-between; align-items: center; }");
                    sb.AppendLine("    .header h1 { margin: 0; font-size: 24px; color: #6366F1; }");
                    sb.AppendLine("    .header p { margin: 5px 0 0 0; font-size: 13px; color: #94A3B8; }");
                    sb.AppendLine("    .grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 15px; margin-bottom: 25px; }");
                    sb.AppendLine("    .card { background-color: #0F172A; border-radius: 8px; padding: 15px; border: 1px solid #334155; text-align: center; }");
                    sb.AppendLine("    .card h3 { margin: 0 0 5px 0; font-size: 12px; color: #94A3B8; text-transform: uppercase; }");
                    sb.AppendLine("    .card .val { font-size: 22px; font-weight: bold; color: #F8FAFC; }");
                    sb.AppendLine("    .section-title { font-size: 16px; font-weight: bold; margin: 25px 0 12px 0; color: #38BDF8; border-bottom: 1px solid #334155; padding-bottom: 6px; }");
                    sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin-top: 10px; font-size: 13px; }");
                    sb.AppendLine("    th { background-color: #0F172A; color: #94A3B8; text-align: left; padding: 10px; border-bottom: 1px solid #334155; }");
                    sb.AppendLine("    td { padding: 10px; border-bottom: 1px solid #334155; color: #E2E8F0; }");
                    sb.AppendLine("    .badge { padding: 3px 8px; border-radius: 4px; font-size: 11px; font-weight: bold; }");
                    sb.AppendLine("    .badge-ok { background-color: #064E3B; color: #34D399; }");
                    sb.AppendLine("    .badge-warn { background-color: #78350F; color: #FBBF24; }");
                    sb.AppendLine("    .footer { margin-top: 30px; text-align: center; font-size: 11px; color: #64748B; border-top: 1px solid #334155; padding-top: 15px; }");
                    sb.AppendLine("  </style>");
                    sb.AppendLine("</head>");
                    sb.AppendLine("<body>");
                    sb.AppendLine("  <div class='container'>");
                    
                    // Header
                    sb.AppendLine("    <div class='header'>");
                    sb.AppendLine("      <div>");
                    sb.AppendLine("        <h1>WinCleaner - Reporte de Diagnóstico</h1>");
                    sb.AppendLine($"        <p>Generado el {DateTime.Now:dd/MM/yyyy a las HH:mm:ss}</p>");
                    sb.AppendLine("      </div>");
                    sb.AppendLine("      <div class='badge badge-ok'>Estado del Sistema: Saludable</div>");
                    sb.AppendLine("    </div>");

                    // Cards principales
                    sb.AppendLine("    <div class='grid'>");
                    sb.AppendLine("      <div class='card'>");
                    sb.AppendLine("        <h3>USO PROCESADOR (CPU)</h3>");
                    sb.AppendLine($"        <div class='val'>{metrics.Cpu.UsagePercentage:F0}%</div>");
                    sb.AppendLine("      </div>");
                    sb.AppendLine("      <div class='card'>");
                    sb.AppendLine("        <h3>MEMORIA RAM LIBRE</h3>");
                    sb.AppendLine($"        <div class='val'>{metrics.Ram.AvailableGb:F1} GB</div>");
                    sb.AppendLine("      </div>");
                    sb.AppendLine("      <div class='card'>");
                    sb.AppendLine("        <h3>ESPACIO LIBERADO TOTAL</h3>");
                    sb.AppendLine($"        <div class='val'>{totalCleanedText}</div>");
                    sb.AppendLine("      </div>");
                    sb.AppendLine("    </div>");

                    // Sección Hardware y Temperaturas
                    sb.AppendLine("    <div class='section-title'>🌡️ Sensores de Temperatura e Identificación de Hardware</div>");
                    sb.AppendLine("    <table>");
                    sb.AppendLine("      <thead><tr><th>Componente / Modelo</th><th>Temperatura / Valor</th><th>Estado Térmico</th></tr></thead>");
                    sb.AppendLine("      <tbody>");
                    foreach (var temp in temps)
                    {
                        string badgeClass = temp.Status == "Caliente" ? "badge-warn" : "badge-ok";
                        sb.AppendLine("        <tr>");
                        sb.AppendLine($"          <td><strong>{temp.ComponentName}</strong><br><small style='color:#94A3B8;'>{temp.ModelName}</small></td>");
                        sb.AppendLine($"          <td>{temp.ValueText}</td>");
                        sb.AppendLine($"          <td><span class='badge {badgeClass}'>{temp.Status}</span></td>");
                        sb.AppendLine("        </tr>");
                    }
                    sb.AppendLine("      </tbody>");
                    sb.AppendLine("    </table>");

                    // Sección Inicio de Windows
                    sb.AppendLine("    <div class='section-title'>🚀 Aplicaciones en el Inicio de Windows</div>");
                    sb.AppendLine("    <table>");
                    sb.AppendLine("      <thead><tr><th>Nombre de la Aplicación</th><th>Estado</th><th>Impacto Estimado</th></tr></thead>");
                    sb.AppendLine("      <tbody>");
                    foreach (var app in startupApps.Take(8))
                    {
                        string statusBadge = app.IsEnabled ? "<span class='badge badge-ok'>Habilitado</span>" : "<span class='badge badge-warn'>Deshabilitado</span>";
                        sb.AppendLine("        <tr>");
                        sb.AppendLine($"          <td><strong>{app.Name}</strong></td>");
                        sb.AppendLine($"          <td>{statusBadge}</td>");
                        sb.AppendLine($"          <td>{app.Impact}</td>");
                        sb.AppendLine("        </tr>");
                    }
                    sb.AppendLine("      </tbody>");
                    sb.AppendLine("    </table>");

                    // Footer
                    sb.AppendLine("    <div class='footer'>");
                    sb.AppendLine("      WinCleaner v1.10.0 &bull; Informe de Diagnóstico Profesional &bull; Desarrollado con .NET 9 & WPF");
                    sb.AppendLine("    </div>");

                    sb.AppendLine("  </div>");
                    sb.AppendLine("</body>");
                    sb.AppendLine("</html>");

                    File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

                    // Abrir en el navegador predeterminado
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = filePath,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "No se pudo abrir automáticamente el reporte HTML.");
                    }

                    return filePath;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al generar el reporte HTML de diagnóstico.");
                    throw;
                }
            });
        }
    }
}
