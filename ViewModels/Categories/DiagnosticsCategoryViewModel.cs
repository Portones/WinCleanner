using System;

namespace WinCleaner.ViewModels.Categories
{
    public class DiagnosticsCategoryViewModel : ViewModelBase
    {
        public TemperatureViewModel TemperatureViewModel { get; }
        public BatteryViewModel BatteryViewModel { get; }
        public DriverViewModel DriverViewModel { get; }
        public CrashInspectorViewModel CrashInspectorViewModel { get; }

        public DiagnosticsCategoryViewModel(
            TemperatureViewModel temperatureViewModel,
            BatteryViewModel batteryViewModel,
            DriverViewModel driverViewModel,
            CrashInspectorViewModel crashInspectorViewModel)
        {
            TemperatureViewModel = temperatureViewModel ?? throw new ArgumentNullException(nameof(temperatureViewModel));
            BatteryViewModel = batteryViewModel ?? throw new ArgumentNullException(nameof(batteryViewModel));
            DriverViewModel = driverViewModel ?? throw new ArgumentNullException(nameof(driverViewModel));
            CrashInspectorViewModel = crashInspectorViewModel ?? throw new ArgumentNullException(nameof(crashInspectorViewModel));
        }
    }
}
