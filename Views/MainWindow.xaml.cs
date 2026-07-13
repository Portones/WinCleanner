using System.Diagnostics;
using System.Windows;
using WinCleaner.ViewModels;

namespace WinCleaner.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
            }
            catch (System.Exception ex)
            {
                Serilog.Log.Error(ex, "Error al abrir el enlace {Url}", e.Uri.AbsoluteUri);
            }
            e.Handled = true;
        }
    }
}
