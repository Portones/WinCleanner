using System;

namespace WinCleaner.ViewModels.Categories
{
    public class AppCategoryViewModel : ViewModelBase
    {
        public UninstallerViewModel UninstallerViewModel { get; }
        public UpdaterViewModel UpdaterViewModel { get; }
        public RuntimeInstallerViewModel RuntimeInstallerViewModel { get; }

        public AppCategoryViewModel(
            UninstallerViewModel uninstallerViewModel,
            UpdaterViewModel updaterViewModel,
            RuntimeInstallerViewModel runtimeInstallerViewModel)
        {
            UninstallerViewModel = uninstallerViewModel ?? throw new ArgumentNullException(nameof(uninstallerViewModel));
            UpdaterViewModel = updaterViewModel ?? throw new ArgumentNullException(nameof(updaterViewModel));
            RuntimeInstallerViewModel = runtimeInstallerViewModel ?? throw new ArgumentNullException(nameof(runtimeInstallerViewModel));
        }
    }
}
