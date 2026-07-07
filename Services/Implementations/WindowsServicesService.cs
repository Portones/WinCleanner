using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class WindowsServicesService : IWindowsServicesService
    {
        // Whitelist de servicios críticos que NO deben pararse ni modificarse bajo ningún concepto
        private static readonly HashSet<string> CriticalServices = new(StringComparer.OrdinalIgnoreCase)
        {
            "RpcSs", "EventLog", "DcomLaunch", "SamSs", "LSM", "PlugPlay", 
            "StateRepository", "mpssvc", "Themes", "ProfSvc", "winmgmt", 
            "gpsvc", "CryptSvc", "RpcEptMapper", "KeyIso", "BrokerInfrastructure"
        };

        private const string ServicesKeyPath = @"SYSTEM\CurrentControlSet\Services";

        public async Task<List<ServiceItem>> GetServicesAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var list = new List<ServiceItem>();
                try
                {
                    var controllers = ServiceController.GetServices();
                    foreach (var sc in controllers)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        bool isCritical = CriticalServices.Contains(sc.ServiceName);
                        bool isRunning = sc.Status == ServiceControllerStatus.Running;

                        // Determinar tipo de inicio (Automatic, Manual, Disabled)
                        string startTypeStr = MapStartMode(sc.StartType);

                        list.Add(new ServiceItem
                        {
                            Name = sc.ServiceName,
                            DisplayName = sc.DisplayName,
                            IsRunning = isRunning,
                            StartType = startTypeStr,
                            CanStop = sc.CanStop && !isCritical,
                            IsCritical = isCritical
                        });

                        sc.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al enumerar servicios de Windows.");
                }

                return list.OrderBy(x => x.DisplayName).ToList();
            }, cancellationToken);
        }

        public async Task<bool> StartServiceAsync(string serviceName, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var sc = new ServiceController(serviceName))
                    {
                        if (sc.Status == ServiceControllerStatus.Stopped)
                        {
                            sc.Start();
                            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(8));
                            Log.Information("Servicio {Service} iniciado correctamente.", serviceName);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al iniciar el servicio {Service}", serviceName);
                    throw; // Lanzar para que el ViewModel lo capture y avise de falta de permisos
                }
                return false;
            }, cancellationToken);
        }

        public async Task<bool> StopServiceAsync(string serviceName, CancellationToken cancellationToken)
        {
            if (CriticalServices.Contains(serviceName))
            {
                throw new InvalidOperationException("No se puede detener un servicio crítico del sistema.");
            }

            return await Task.Run(() =>
            {
                try
                {
                    using (var sc = new ServiceController(serviceName))
                    {
                        if (sc.Status == ServiceControllerStatus.Running && sc.CanStop)
                        {
                            sc.Stop();
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(8));
                            Log.Information("Servicio {Service} detenido correctamente.", serviceName);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al detener el servicio {Service}", serviceName);
                    throw;
                }
                return false;
            }, cancellationToken);
        }

        public async Task<bool> ChangeServiceStartTypeAsync(string serviceName, string startType, CancellationToken cancellationToken)
        {
            if (CriticalServices.Contains(serviceName))
            {
                throw new InvalidOperationException("No se puede modificar el tipo de inicio de un servicio crítico.");
            }

            return await Task.Run(() =>
            {
                try
                {
                    int startValue = startType switch
                    {
                        "Automático" => 2,
                        "Manual" => 3,
                        "Deshabilitado" => 4,
                        _ => 3
                    };

                    using (var key = Registry.LocalMachine.OpenSubKey($@"{ServicesKeyPath}\{serviceName}", true))
                    {
                        if (key != null)
                        {
                            key.SetValue("Start", startValue, RegistryValueKind.DWord);
                            Log.Information("Cambio tipo inicio del servicio {Service} a {Type} (Valor: {Val}).", serviceName, startType, startValue);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al modificar tipo de inicio del servicio {Service}", serviceName);
                    throw;
                }
                return false;
            }, cancellationToken);
        }

        private static string MapStartMode(ServiceStartMode mode)
        {
            return mode switch
            {
                ServiceStartMode.Automatic => "Automático",
                ServiceStartMode.Manual => "Manual",
                ServiceStartMode.Disabled => "Deshabilitado",
                ServiceStartMode.System => "Sistema (Driver)",
                ServiceStartMode.Boot => "Arranque (Driver)",
                _ => "Manual"
            };
        }
    }
}
