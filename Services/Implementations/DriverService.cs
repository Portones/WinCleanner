using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class DriverService : IDriverService
    {
        public async Task<List<DriverItem>> GetInstalledDriversAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<DriverItem>();
                try
                {
                    using (var searcher = new ManagementObjectSearcher(@"root\CIMV2", 
                        "SELECT DeviceName, DriverVersion, DriverProviderName, DriverDate, DeviceClass, Signer FROM Win32_PnPSignedDriver WHERE DeviceName IS NOT NULL"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            string deviceName = obj["DeviceName"]?.ToString() ?? string.Empty;
                            string driverVersion = obj["DriverVersion"]?.ToString() ?? string.Empty;
                            string provider = obj["DriverProviderName"]?.ToString() ?? string.Empty;
                            string rawDate = obj["DriverDate"]?.ToString() ?? string.Empty;
                            string deviceClass = obj["DeviceClass"]?.ToString() ?? string.Empty;
                            string signer = obj["Signer"]?.ToString() ?? string.Empty;

                            if (string.IsNullOrEmpty(deviceClass)) continue;

                            // Filtrar controladores virtuales o genéricos de Microsoft para evitar ruido excesivo
                            if (provider.Equals("Microsoft", StringComparison.OrdinalIgnoreCase) && 
                                (deviceClass.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) || 
                                 deviceClass.Equals("VOLUME", StringComparison.OrdinalIgnoreCase) ||
                                 deviceClass.Equals("PROCESSOR", StringComparison.OrdinalIgnoreCase) ||
                                 deviceName.Contains("Root Print Queue") || 
                                 deviceName.Contains("Software Device") ||
                                 deviceName.Contains("Volume Manager")))
                            {
                                continue;
                            }

                            DateTime driverDate = ParseCimDateTime(rawDate);
                            string dateText = driverDate == DateTime.MinValue ? "Desconocido" : driverDate.ToString("dd/MM/yyyy");

                            // Antiguo si tiene más de 3 años (1095 días)
                            bool isOutdated = driverDate != DateTime.MinValue && (DateTime.Now - driverDate).TotalDays > 1095;

                            list.Add(new DriverItem
                            {
                                DeviceName = deviceName,
                                DriverVersion = driverVersion,
                                DriverProvider = provider,
                                DriverDate = driverDate,
                                DriverDateString = dateText,
                                DeviceClass = deviceClass.ToUpper(),
                                Signer = signer,
                                IsOutdated = isOutdated
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al consultar controladores firmados mediante WMI.");
                }

                return list;
            });
        }

        public async Task<List<DriverUpdateItem>> GetAvailableDriverUpdatesAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<DriverUpdateItem>();
                try
                {
                    // Crear sesión de actualización dinámica de Windows Update COM
                    Type? sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
                    if (sessionType == null) throw new InvalidOperationException("No se pudo obtener el ProgID de Microsoft.Update.Session");

                    dynamic? session = Activator.CreateInstance(sessionType);
                    if (session == null) throw new InvalidOperationException("No se pudo crear la instancia de Microsoft.Update.Session");

                    dynamic searcher = session.CreateUpdateSearcher();
                    
                    // Buscar actualizaciones no instaladas de tipo Driver
                    dynamic result = searcher.Search("IsInstalled=0 and Type='Driver'");
                    dynamic updates = result.Updates;

                    int count = updates.Count;
                    for (int i = 0; i < count; i++)
                    {
                        dynamic update = updates.Item(i);
                        string title = update.Title;
                        string description = update.Description ?? "No hay descripción disponible para esta actualización.";
                        string updateId = update.Identity.UpdateID;

                        list.Add(new DriverUpdateItem
                        {
                            Title = title,
                            Description = description,
                            UpdateId = updateId
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "El Agente de Windows Update no está disponible o no tiene actualizaciones de controladores pendientes.");
                }

                return list;
            });
        }

        private DateTime ParseCimDateTime(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 8)
                return DateTime.MinValue;

            try
            {
                int year = int.Parse(value.Substring(0, 4));
                int month = int.Parse(value.Substring(4, 2));
                int day = int.Parse(value.Substring(6, 2));
                return new DateTime(year, month, day);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }
}
