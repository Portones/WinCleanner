using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class SystemRepairService : ISystemRepairService
    {
        private static readonly Regex PercentageRegex = new Regex(@"(\d+(?:\.\d+)?)\s*%", RegexOptions.Compiled);

        public async Task RunSfcScanAsync(IProgress<string> outputProgress, IProgress<double> percentProgress, CancellationToken cancellationToken)
        {
            outputProgress.Report("=== Iniciando Comprobador de Archivos de Sistema (SFC) ===");
            outputProgress.Report("Ejecutando: sfc /scannow ...\n");

            await RunProcessAsync("sfc.exe", "/scannow", outputProgress, percentProgress, cancellationToken);

            outputProgress.Report("\n=== Proceso SFC Finalizado ===");
        }

        public async Task RunDismRepairAsync(IProgress<string> outputProgress, IProgress<double> percentProgress, CancellationToken cancellationToken)
        {
            outputProgress.Report("=== Iniciando Reparación de Imagen del Sistema (DISM) ===");
            outputProgress.Report("Ejecutando: dism /online /cleanup-image /restorehealth ...\n");

            await RunProcessAsync("dism.exe", "/online /cleanup-image /restorehealth", outputProgress, percentProgress, cancellationToken);

            outputProgress.Report("\n=== Proceso DISM Finalizado ===");
        }

        private async Task RunProcessAsync(string fileName, string arguments, IProgress<string> outputProgress, IProgress<double> percentProgress, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.GetEncoding(850),
                StandardErrorEncoding = Encoding.GetEncoding(850)
            };

            using var process = new Process { StartInfo = psi };

            DataReceivedEventHandler outputHandler = (sender, e) =>
            {
                if (e.Data != null)
                {
                    outputProgress.Report(e.Data);

                    var match = PercentageRegex.Match(e.Data);
                    if (match.Success && double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double percent))
                    {
                        percentProgress.Report(percent);
                    }
                }
            };

            process.OutputDataReceived += outputHandler;
            process.ErrorDataReceived += outputHandler;

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using (cancellationToken.Register(() =>
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(true);
                            outputProgress.Report("\n[!] Operación cancelada por el usuario.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error al intentar finalizar el proceso {FileName}", fileName);
                    }
                }))
                {
                    await process.WaitForExitAsync(cancellationToken);
                }

                outputProgress.Report($"\nProceso finalizado con código de salida: {process.ExitCode}");
                if (process.ExitCode == 0)
                {
                    percentProgress.Report(100);
                }
            }
            catch (OperationCanceledException)
            {
                outputProgress.Report("\n[!] La operación fue cancelada.");
                Log.Information("Operación {FileName} cancelada por el usuario.", fileName);
            }
            catch (Exception ex)
            {
                outputProgress.Report($"\n[ERROR] Ocurrió un fallo al ejecutar {fileName}: {ex.Message}");
                Log.Error(ex, "Error al ejecutar el proceso de reparación {FileName}", fileName);
            }
        }
    }
}
