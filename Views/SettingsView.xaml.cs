using System.Windows.Controls;
using WinCleaner.ViewModels;

namespace WinCleaner.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            Loaded += SettingsView_Loaded;
        }

        private void SettingsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel viewModel)
            {
                if (viewModel.IsUpdateAvailable)
                {
                    // Hacer scroll automático al final si hay una actualización pendiente de descargar
                    SettingsScrollViewer.ScrollToEnd();
                }
            }
        }
    }
}
