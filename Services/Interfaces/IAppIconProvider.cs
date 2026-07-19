using System.Windows.Media;

namespace WinCleaner.Services.Interfaces
{
    public interface IAppIconProvider
    {
        ImageSource? GetAppIcon(string iconPath, string installLocation);
    }
}
