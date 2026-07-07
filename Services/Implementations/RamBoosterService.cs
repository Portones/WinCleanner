using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class RamBoosterService : IRamBoosterService
    {
        // P/Invoke para OpenProcess
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        // P/Invoke para CloseHandle
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        // P/Invoke para vaciar el espacio de trabajo del proceso
        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        // Constantes de acceso a procesos
        private const uint PROCESS_SET_QUOTA = 0x0100;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_READ = 0x0010;

        // Estructura para consultar estado de memoria global
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
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
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        public async Task<long> OptimizeRamAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                progress.Report(0);

                // 1. Obtener la memoria física disponible inicial
                ulong initialAvail = GetAvailableMemory();
                Log.Information("Iniciando Smart RAM Booster. Memoria disponible inicial: {Bytes} bytes.", initialAvail);

                // 2. Obtener la lista de todos los procesos activos
                var processes = Process.GetProcesses();
                int totalProcesses = processes.Length;
                int processedCount = 0;
                int optimizedCount = 0;

                foreach (var process in processes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Omitir el proceso inactivo del sistema (Idle) y el proceso System
                    if (process.Id == 0 || process.Id == 4)
                    {
                        processedCount++;
                        continue;
                    }

                    IntPtr hProcess = IntPtr.Zero;
                    try
                    {
                        // Abrir proceso con permisos para vaciar espacio de trabajo
                        hProcess = OpenProcess(PROCESS_SET_QUOTA | PROCESS_QUERY_INFORMATION, false, process.Id);
                        if (hProcess == IntPtr.Zero)
                        {
                            // Reintentar con VM_READ en lugar de QUERY_INFORMATION por si acaso
                            hProcess = OpenProcess(PROCESS_SET_QUOTA | PROCESS_VM_READ, false, process.Id);
                        }

                        if (hProcess != IntPtr.Zero)
                        {
                            bool success = EmptyWorkingSet(hProcess);
                            if (success)
                            {
                                optimizedCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Verbose("No se pudo optimizar el proceso {Name} (PID {Pid}): {Msg}", process.ProcessName, process.Id, ex.Message);
                    }
                    finally
                    {
                        if (hProcess != IntPtr.Zero)
                        {
                            CloseHandle(hProcess);
                        }
                        process.Dispose(); // Liberar recursos del objeto Process
                    }

                    processedCount++;
                    if (processedCount % 10 == 0)
                    {
                        progress.Report((double)processedCount / totalProcesses * 100);
                    }
                }

                // 3. Obtener la memoria física disponible final
                ulong finalAvail = GetAvailableMemory();
                long bytesFreed = (long)finalAvail - (long)initialAvail;

                Log.Information("RAM Booster finalizado. Optimizado {Count} de {Total} procesos. Memoria liberada: {Freed} bytes.", 
                                optimizedCount, totalProcesses, bytesFreed > 0 ? bytesFreed : 0);

                progress.Report(100);

                // Retornar la cantidad de bytes liberados (asegurar mínimo de 0)
                return bytesFreed > 0 ? bytesFreed : 0;

            }, cancellationToken);
        }

        private ulong GetAvailableMemory()
        {
            var memoryStatus = new MEMORYSTATUSEX();
            memoryStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));

            if (GlobalMemoryStatusEx(ref memoryStatus))
            {
                return memoryStatus.ullAvailPhys;
            }
            return 0;
        }
    }
}
