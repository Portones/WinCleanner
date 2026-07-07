using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface IConfigurationService
    {
        AppSettings CurrentSettings { get; }
        void LoadSettings();
        void SaveSettings();
        void ResetToDefault();
    }
}
