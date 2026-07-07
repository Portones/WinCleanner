using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Win32;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class SystemDiagnosticService : ISystemDiagnosticService
    {
        // P/Invoke para obtener el uso de CPU (GetSystemTimes)
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

        // P/Invoke para obtener el estado de la RAM (GlobalMemoryStatusEx)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        // Variables para cálculo de CPU
        private FILETIME _prevIdleTime;
        private FILETIME _prevKernelTime;
        private FILETIME _prevUserTime;
        private DateTime _lastCpuSampleTime = DateTime.MinValue;
        private double _lastCpuUsage = 0;

        // Variables para caché de estado SMART de discos
        private readonly Dictionary<string, string> _smartCache = new(StringComparer.OrdinalIgnoreCase);
        private DateTime _lastSmartUpdate = DateTime.MinValue;

        public CpuMetrics GetCpuMetrics()
        {
            var metrics = new CpuMetrics();
            try
            {
                metrics.UsagePercentage = CalculateCpuUsage();
                metrics.TemperatureText = GetCpuTemperature();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al obtener métricas de CPU.");
            }
            return metrics;
        }

        public RamMetrics GetRamMetrics()
        {
            var metrics = new RamMetrics();
            try
            {
                var memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    double bytesToGb = 1024.0 * 1024.0 * 1024.0;
                    metrics.TotalGb = memStatus.ullTotalPhys / bytesToGb;
                    metrics.AvailableGb = memStatus.ullAvailPhys / bytesToGb;
                    metrics.UsedGb = metrics.TotalGb - metrics.AvailableGb;
                    metrics.UsagePercentage = memStatus.dwMemoryLoad;

                    // Memoria comprometida
                    var committedBytes = memStatus.ullTotalPageFile - memStatus.ullAvailPageFile;
                    metrics.CommittedGb = committedBytes / bytesToGb;
                    metrics.CommittedLimitGb = memStatus.ullTotalPageFile / bytesToGb;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al obtener métricas de RAM.");
            }
            return metrics;
        }

        public List<DiskMetrics> GetDiskMetrics()
        {
            var list = new List<DiskMetrics>();
            try
            {
                var drives = DriveInfo.GetDrives();
                foreach (var drive in drives)
                {
                    if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                    {
                        var metric = new DiskMetrics
                        {
                            DriveLetter = drive.Name,
                            VolumeLabel = drive.VolumeLabel,
                            TotalSizeGb = drive.TotalSize / (1024.0 * 1024.0 * 1024.0),
                            FreeSpaceGb = drive.TotalFreeSpace / (1024.0 * 1024.0 * 1024.0),
                            DriveType = drive.DriveType.ToString(),
                            SmartStatus = GetSmartStatusForDrive(drive.Name)
                        };
                        list.Add(metric);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al obtener métricas de Discos.");
            }
            return list;
        }

        public SystemHealthMetrics GetAllMetrics()
        {
            var metrics = new SystemHealthMetrics
            {
                Cpu = GetCpuMetrics(),
                Ram = GetRamMetrics(),
                Disks = GetDiskMetrics(),
                Gpus = GetGpuMetrics()
            };

            // Calcular salud general basada en uso de recursos
            metrics.OverallHealthStatus = DetermineOverallHealth(metrics);
            metrics.UptimeText = GetSystemUptime();

            return metrics;
        }

        public List<GpuMetrics> GetGpuMetrics()
        {
            var list = new List<GpuMetrics>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, DriverVersion, AdapterRAM FROM Win32_VideoController");
                foreach (ManagementObject gpu in searcher.Get())
                {
                    string name = gpu["Name"]?.ToString() ?? "GPU desconocida";
                    // Filtrar el Microsoft Basic Display Adapter (no es GPU real)
                    if (name.Contains("Microsoft Basic", StringComparison.OrdinalIgnoreCase)) continue;

                    ulong adapterRamBytes = 0;
                    try { adapterRamBytes = Convert.ToUInt64(gpu["AdapterRAM"]); } catch { }
                    string adapterRam = adapterRamBytes > 0
                        ? $"{adapterRamBytes / 1024.0 / 1024.0 / 1024.0:F1} GB VRAM"
                        : "VRAM desconocida";

                    list.Add(new GpuMetrics
                    {
                        Name = name,
                        DriverVersion = gpu["DriverVersion"]?.ToString() ?? string.Empty,
                        AdapterRam = adapterRam,
                        UsagePercentage = 0 // WMI no expone GPU% directamente sin DXGI/PDH
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Warning("No se pudieron obtener datos de GPU: {Msg}", ex.Message);
            }
            return list;
        }

        private double CalculateCpuUsage()
        {
            if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
            {
                return 0;
            }

            var now = DateTime.UtcNow;
            if (_lastCpuSampleTime == DateTime.MinValue)
            {
                _prevIdleTime = idleTime;
                _prevKernelTime = kernelTime;
                _prevUserTime = userTime;
                _lastCpuSampleTime = now;
                return 0;
            }

            var idleDiff = ConvertFileTimeToUInt64(idleTime) - ConvertFileTimeToUInt64(_prevIdleTime);
            var kernelDiff = ConvertFileTimeToUInt64(kernelTime) - ConvertFileTimeToUInt64(_prevKernelTime);
            var userDiff = ConvertFileTimeToUInt64(userTime) - ConvertFileTimeToUInt64(_prevUserTime);

            _prevIdleTime = idleTime;
            _prevKernelTime = kernelTime;
            _prevUserTime = userTime;
            _lastCpuSampleTime = now;

            var totalSystem = kernelDiff + userDiff;
            if (totalSystem == 0) return _lastCpuUsage;

            var usage = 1.0 - ((double)idleDiff / totalSystem);
            _lastCpuUsage = usage * 100.0;
            
            if (_lastCpuUsage < 0) _lastCpuUsage = 0;
            if (_lastCpuUsage > 100) _lastCpuUsage = 100;

            return _lastCpuUsage;
        }

        private static ulong ConvertFileTimeToUInt64(FILETIME ft)
        {
            return ((ulong)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
        }

        private string GetCpuTemperature()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var tempKelvin = Convert.ToDouble(obj["CurrentTemperature"]);
                        var tempCelsius = (tempKelvin / 10.0) - 273.15;
                        return $"{tempCelsius:F0}°C";
                    }
                }
            }
            catch
            {
                // Ignorar silenciosamente (muchas placas no lo implementan o requiere Admin elevado)
            }
            return "No disponible";
        }

        private string GetSmartStatusForDrive(string driveLetter)
        {
            var cleanLetter = driveLetter.TrimEnd('\\');
            
            // Si la última actualización de SMART fue hace menos de 5 minutos, usar la caché
            if (DateTime.UtcNow - _lastSmartUpdate < TimeSpan.FromMinutes(5) && _smartCache.TryGetValue(cleanLetter, out var cachedStatus))
            {
                return cachedStatus;
            }

            try
            {
                string query = $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{cleanLetter}'}} WHERE AssocClass=Win32_LogicalDiskToPartition";
                
                using (var searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject partition in searcher.Get())
                    {
                        var partitionId = partition["DeviceID"]?.ToString();
                        if (string.IsNullOrEmpty(partitionId)) continue;

                        string diskQuery = $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partitionId}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition";
                        using (var diskSearcher = new ManagementObjectSearcher(diskQuery))
                        {
                            foreach (ManagementObject disk in diskSearcher.Get())
                            {
                                var status = disk["Status"]?.ToString() ?? "OK";
                                var result = status == "OK" ? "Saludable (OK)" : status;
                                _smartCache[cleanLetter] = result;
                                _lastSmartUpdate = DateTime.UtcNow;
                                return result;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("No se pudo obtener estado SMART WMI para {Drive}: {Msg}", driveLetter, ex.Message);
            }

            // Si falla, usar valor previo de caché si existe, o por defecto "Saludable (OK)"
            if (_smartCache.TryGetValue(cleanLetter, out var lastStatus))
            {
                return lastStatus;
            }

            _smartCache[cleanLetter] = "Saludable (OK)";
            return "Saludable (OK)";
        }

        private string GetSystemUptime()
        {
            try
            {
                var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
            }
            catch
            {
                return "No disponible";
            }
        }

        private string DetermineOverallHealth(SystemHealthMetrics metrics)
        {
            // Evaluación simple de salud basada en uso
            double score = 100;

            if (metrics.Cpu.UsagePercentage > 85) score -= 15;
            else if (metrics.Cpu.UsagePercentage > 60) score -= 5;

            if (metrics.Ram.UsagePercentage > 90) score -= 25;
            else if (metrics.Ram.UsagePercentage > 75) score -= 10;

            foreach (var disk in metrics.Disks)
            {
                if (disk.UsedPercentage > 95) score -= 20;
                else if (disk.UsedPercentage > 85) score -= 10;

                if (disk.SmartStatus != "Saludable (OK)" && disk.SmartStatus != "No disponible")
                {
                    score -= 50; // Problema físico grave
                }
            }

            if (score >= 90) return "Excelente";
            if (score >= 70) return "Bueno";
            if (score >= 50) return "Requiere Optimización";
            return "Crítico";
        }

        public bool IsRebootRequired()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired", false))
                {
                    if (key != null) return true;
                }

                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending", false))
                {
                    if (key != null) return true;
                }

                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager", false))
                {
                    var val = key?.GetValue("PendingFileRenameOperations");
                    if (val != null)
                    {
                        if (val is string[] arr && arr.Length > 0 && !string.IsNullOrWhiteSpace(arr[0]))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error al comprobar estado de reinicio pendiente: {Msg}", ex.Message);
            }
            return false;
        }

        public TimeSpan GetUptime()
        {
            return TimeSpan.FromMilliseconds(Environment.TickCount64);
        }
    }
}
