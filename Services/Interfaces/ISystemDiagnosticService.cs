using System;
using System.Collections.Generic;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface ISystemDiagnosticService
    {
        CpuMetrics GetCpuMetrics();
        RamMetrics GetRamMetrics();
        List<DiskMetrics> GetDiskMetrics();
        List<GpuMetrics> GetGpuMetrics();
        SystemHealthMetrics GetAllMetrics();
        bool IsRebootRequired();
        TimeSpan GetUptime();
    }
}
