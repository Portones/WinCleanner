using System;

namespace WinCleaner.ViewModels.Categories
{
    public class DiskCategoryViewModel : ViewModelBase
    {
        public DiskAnalyzerViewModel DiskAnalyzerViewModel { get; }
        public SsdOptimizerViewModel SsdOptimizerViewModel { get; }

        public DiskCategoryViewModel(
            DiskAnalyzerViewModel diskAnalyzerViewModel,
            SsdOptimizerViewModel ssdOptimizerViewModel)
        {
            DiskAnalyzerViewModel = diskAnalyzerViewModel ?? throw new ArgumentNullException(nameof(diskAnalyzerViewModel));
            SsdOptimizerViewModel = ssdOptimizerViewModel ?? throw new ArgumentNullException(nameof(ssdOptimizerViewModel));
        }
    }
}
