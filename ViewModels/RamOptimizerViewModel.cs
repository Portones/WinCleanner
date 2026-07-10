using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.ViewModels
{
    public class RamOptimizerViewModel : ViewModelBase
    {
        private readonly IRamBoosterService _ramBooster;
        private DispatcherTimer _timer;
        private Dictionary<int, (TimeSpan time, DateTime date)> _processCpuHistory = new();

        private ObservableCollection<ActiveProcessItem> _processes = new();
        private string _searchText = string.Empty;
        private string _statusMessage = "Listo";
        private bool _isOptimizing;
        private bool _isAutoRefreshEnabled = true;
        private double _optimizationProgress;

        private double _ramUsagePercentage;
        private string _ramUsageText = "Cargando...";
        private long _totalRamBytes;
        private long _availRamBytes;

        // P/Invoke para MEMORYSTATUSEX
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

        public ObservableCollection<ActiveProcessItem> Processes
        {
            get => _processes;
            set => SetProperty(ref _processes, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    UpdateProcessList();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsOptimizing
        {
            get => _isOptimizing;
            set
            {
                if (SetProperty(ref _isOptimizing, value))
                {
                    OnPropertyChanged(nameof(CanOptimize));
                }
            }
        }

        public bool CanOptimize => !_isOptimizing;

        public bool IsAutoRefreshEnabled
        {
            get => _isAutoRefreshEnabled;
            set
            {
                if (SetProperty(ref _isAutoRefreshEnabled, value))
                {
                    if (value) _timer.Start();
                    else _timer.Stop();
                }
            }
        }

        public double OptimizationProgress
        {
            get => _optimizationProgress;
            set => SetProperty(ref _optimizationProgress, value);
        }

        public double RamUsagePercentage
        {
            get => _ramUsagePercentage;
            set => SetProperty(ref _ramUsagePercentage, value);
        }

        public string RamUsageText
        {
            get => _ramUsageText;
            set => SetProperty(ref _ramUsageText, value);
        }

        public ICommand OptimizeRamCommand { get; }
        public ICommand KillProcessCommand { get; }
        public ICommand RefreshCommand { get; }

        public RamOptimizerViewModel(IRamBoosterService ramBooster)
        {
            _ramBooster = ramBooster ?? throw new ArgumentNullException(nameof(ramBooster));

            OptimizeRamCommand = new AsyncRelayCommand(OptimizeRamAsync);
            KillProcessCommand = new AsyncRelayCommand<ActiveProcessItem>(KillProcessAsync);
            RefreshCommand = new RelayCommand(UpdateData);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _timer.Tick += (s, e) => UpdateData();

            UpdateData();
            _timer.Start();
        }

        private void UpdateData()
        {
            UpdateRamMetrics();
            UpdateProcessList();
        }

        private void UpdateRamMetrics()
        {
            var memoryStatus = new MEMORYSTATUSEX();
            memoryStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));

            if (GlobalMemoryStatusEx(ref memoryStatus))
            {
                _totalRamBytes = (long)memoryStatus.ullTotalPhys;
                _availRamBytes = (long)memoryStatus.ullAvailPhys;
                long usedRam = _totalRamBytes - _availRamBytes;

                RamUsagePercentage = memoryStatus.dwMemoryLoad;
                RamUsageText = $"{CleanableItem.FormatSize(usedRam)} en uso de {CleanableItem.FormatSize(_totalRamBytes)} ({RamUsagePercentage}%)";
            }
        }

        private void UpdateProcessList()
        {
            var rawProcesses = Process.GetProcesses();
            var now = DateTime.Now;
            var numCores = Environment.ProcessorCount;
            var newList = new List<ActiveProcessItem>();

            foreach (var p in rawProcesses)
            {
                try
                {
                    if (p.Id == 0 || p.Id == 4) continue; // Omitir System e Idle

                    double cpuPercent = 0;
                    if (_processCpuHistory.TryGetValue(p.Id, out var history))
                    {
                        var curTime = p.TotalProcessorTime;
                        var elapsedMs = (now - history.date).TotalMilliseconds;
                        var processorMs = (curTime - history.time).TotalMilliseconds;
                        if (elapsedMs > 0)
                        {
                            cpuPercent = (processorMs / elapsedMs) / numCores * 100;
                            cpuPercent = Math.Clamp(cpuPercent, 0, 100);
                        }
                        _processCpuHistory[p.Id] = (curTime, now);
                    }
                    else
                    {
                        _processCpuHistory[p.Id] = (p.TotalProcessorTime, now);
                    }

                    var item = new ActiveProcessItem
                    {
                        Id = p.Id,
                        Name = p.ProcessName,
                        MemoryBytes = p.WorkingSet64,
                        CpuPercentage = cpuPercent
                    };
                    newList.Add(item);
                }
                catch
                {
                    // Intentar añadir el proceso solo con RAM si el tiempo de CPU falla (Acceso Denegado)
                    try
                    {
                        var item = new ActiveProcessItem
                        {
                            Id = p.Id,
                            Name = p.ProcessName,
                            MemoryBytes = p.WorkingSet64,
                            CpuPercentage = 0
                        };
                        newList.Add(item);
                    }
                    catch { /* Proceso terminado, omitir */ }
                }
                finally
                {
                    p.Dispose();
                }
            }

            // Limpiar historial de procesos inactivos
            var activeIds = new HashSet<int>(newList.Select(x => x.Id));
            var deadIds = _processCpuHistory.Keys.Where(id => !activeIds.Contains(id)).ToList();
            foreach (var id in deadIds)
            {
                _processCpuHistory.Remove(id);
            }

            // Filtrar y ordenar por memoria descendente
            var query = newList.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            Processes = new ObservableCollection<ActiveProcessItem>(query.OrderByDescending(x => x.MemoryBytes));
        }

        private async Task OptimizeRamAsync()
        {
            if (IsOptimizing) return;

            IsOptimizing = true;
            OptimizationProgress = 0;
            StatusMessage = "Optimizando memoria RAM...";

            try
            {
                var progressReporter = new Progress<double>(val => OptimizationProgress = val);
                long bytesFreed = await _ramBooster.OptimizeRamAsync(progressReporter, CancellationToken.None);

                string freedText = CleanableItem.FormatSize(bytesFreed);
                StatusMessage = $"Optimización completada. Se liberaron {freedText} de RAM.";

                MessageBox.Show($"La optimización de RAM finalizó con éxito.\nSe han liberado {freedText} de memoria física.",
                                "Optimización Completada", MessageBoxButton.OK, MessageBoxImage.Information);
                
                UpdateData();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error al liberar memoria RAM.";
                Log.Error(ex, "Error al optimizar RAM en RamOptimizerViewModel.");
            }
            finally
            {
                IsOptimizing = false;
            }
        }

        private async Task KillProcessAsync(ActiveProcessItem? item)
        {
            if (item == null) return;

            var result = MessageBox.Show($"¿Está seguro de que desea finalizar el proceso '{item.Name}' (PID {item.Id})?\n\nForzar el cierre puede causar pérdida de datos no guardados.",
                                         "Confirmar Finalización de Proceso", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                await Task.Run(() =>
                {
                    using (var p = Process.GetProcessById(item.Id))
                    {
                        p.Kill();
                        p.WaitForExit(3000);
                    }
                });

                StatusMessage = $"Proceso '{item.Name}' finalizado correctamente.";
                UpdateData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo finalizar el proceso '{item.Name}':\n{ex.Message}",
                                "Error al Finalizar Proceso", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "Error al finalizar proceso {Pid}", item.Id);
            }
        }

        public void StopTimer()
        {
            _timer.Stop();
        }

        public void StartTimer()
        {
            if (_isAutoRefreshEnabled)
            {
                _timer.Start();
            }
            UpdateData();
        }
    }
}
