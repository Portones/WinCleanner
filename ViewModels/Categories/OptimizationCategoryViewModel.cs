using System;

namespace WinCleaner.ViewModels.Categories
{
    public class OptimizationCategoryViewModel : ViewModelBase
    {
        public PerformanceViewModel PerformanceViewModel { get; }
        public TcpTweakerViewModel TcpTweakerViewModel { get; }
        public SystemRepairViewModel SystemRepairViewModel { get; }
        public RamOptimizerViewModel RamOptimizerViewModel { get; }
        public StartupViewModel StartupViewModel { get; }
        public ServicesViewModel ServicesViewModel { get; }
        public ContextMenuViewModel ContextMenuViewModel { get; }

        public OptimizationCategoryViewModel(
            PerformanceViewModel performanceViewModel,
            TcpTweakerViewModel tcpTweakerViewModel,
            SystemRepairViewModel systemRepairViewModel,
            RamOptimizerViewModel ramOptimizerViewModel,
            StartupViewModel startupViewModel,
            ServicesViewModel servicesViewModel,
            ContextMenuViewModel contextMenuViewModel)
        {
            PerformanceViewModel = performanceViewModel ?? throw new ArgumentNullException(nameof(performanceViewModel));
            TcpTweakerViewModel = tcpTweakerViewModel ?? throw new ArgumentNullException(nameof(tcpTweakerViewModel));
            SystemRepairViewModel = systemRepairViewModel ?? throw new ArgumentNullException(nameof(systemRepairViewModel));
            RamOptimizerViewModel = ramOptimizerViewModel ?? throw new ArgumentNullException(nameof(ramOptimizerViewModel));
            StartupViewModel = startupViewModel ?? throw new ArgumentNullException(nameof(startupViewModel));
            ServicesViewModel = servicesViewModel ?? throw new ArgumentNullException(nameof(servicesViewModel));
            ContextMenuViewModel = contextMenuViewModel ?? throw new ArgumentNullException(nameof(contextMenuViewModel));
        }
    }
}
