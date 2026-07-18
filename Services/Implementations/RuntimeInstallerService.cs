using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class RuntimeInstallerService : IRuntimeInstallerService
    {
        public List<RuntimeItem> GetAvailableRuntimes()
        {
            return new List<RuntimeItem>
            {
                new RuntimeItem
                {
                    Id = "Microsoft.VCRedist.2015+.x64",
                    Name = "Visual C++ Redistributable 2015-2022 (x64)",
                    Category = "Visual C++",
                    Description = "Librerías C++ esenciales para la ejecución de la mayoría de software y juegos de 64 bits en Windows."
                },
                new RuntimeItem
                {
                    Id = "Microsoft.VCRedist.2015+.x86",
                    Name = "Visual C++ Redistributable 2015-2022 (x86)",
                    Category = "Visual C++",
                    Description = "Librerías C++ requeridas por aplicaciones y juegos de 32 bits."
                },
                new RuntimeItem
                {
                    Id = "Microsoft.DotNet.DesktopRuntime.8",
                    Name = ".NET 8.0 Desktop Runtime (x64)",
                    Category = "Entorno .NET",
                    Description = "Motor de ejecución .NET 8 necesario para aplicaciones WPF y Windows Forms modernas."
                },
                new RuntimeItem
                {
                    Id = "Microsoft.DotNet.DesktopRuntime.9",
                    Name = ".NET 9.0 Desktop Runtime (x64)",
                    Category = "Entorno .NET",
                    Description = "Último motor de ejecución .NET 9 para software de alto rendimiento."
                },
                new RuntimeItem
                {
                    Id = "Microsoft.DirectX",
                    Name = "DirectX End-User Runtime Web Installer",
                    Category = "Gaming & Multimedia",
                    Description = "Librerías multimedia de DirectX para compatibilidad de video y audio en juegos."
                },
                new RuntimeItem
                {
                    Id = "Oracle.JavaRuntimeEnvironment",
                    Name = "Java Runtime Environment (JRE)",
                    Category = "Runtimes",
                    Description = "Entorno de ejecución de Java para aplicaciones web y de escritorio compildas en Java."
                },
                new RuntimeItem
                {
                    Id = "Microsoft.XNARedist",
                    Name = "Microsoft XNA Framework Redistributable 4.0",
                    Category = "Gaming & Multimedia",
                    Description = "Componente de ejecución para juegos creados con el motor Microsoft XNA."
                }
            };
        }

        public async Task<bool> InstallRuntimeAsync(RuntimeItem item, IProgress<string> logProgress, CancellationToken cancellationToken)
        {
            logProgress.Report($"\n[INFO] Iniciando instalación de {item.Name} (ID: {item.Id})...");
            Log.Information("Iniciando instalación de paquete Winget: {Id}", item.Id);

            item.Status = "Instalando";

            return await Task.Run(() =>
            {
                try
                {
                    string arguments = $"install --id \"{item.Id}\" --silent --accept-package-agreements --accept-source-agreements";

                    var psi = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };

                    using var process = new Process { StartInfo = psi };

                    DataReceivedEventHandler handler = (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            logProgress.Report(e.Data);
                        }
                    };

                    process.OutputDataReceived += handler;
                    process.ErrorDataReceived += handler;

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
                                logProgress.Report($"\n[!] Instalación de {item.Name} cancelada por el usuario.");
                            }
                        }
                        catch { }
                    }))
                    {
                        process.WaitForExit();
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    bool success = process.ExitCode == 0;
                    if (success)
                    {
                        item.Status = "Instalado";
                        logProgress.Report($"[✅ ÉXITO] {item.Name} se instaló correctamente.");
                        Log.Information("Instalación exitosa de {Id}", item.Id);
                    }
                    else
                    {
                        item.Status = "Fallido";
                        logProgress.Report($"[❌ ERROR] La instalación de {item.Name} finalizó con código de salida: {process.ExitCode}");
                        Log.Warning("Fallo en instalación de {Id}. Código: {ExitCode}", item.Id, process.ExitCode);
                    }

                    return success;
                }
                catch (OperationCanceledException)
                {
                    item.Status = "Cancelado";
                    logProgress.Report($"\n[!] Operación cancelada para {item.Name}.");
                    return false;
                }
                catch (Exception ex)
                {
                    item.Status = "Fallido";
                    logProgress.Report($"[❌ ERROR] Fallo crítico al instalar {item.Name}: {ex.Message}");
                    Log.Error(ex, "Excepción al instalar {Id}", item.Id);
                    return false;
                }
            }, cancellationToken);
        }
    }
}
