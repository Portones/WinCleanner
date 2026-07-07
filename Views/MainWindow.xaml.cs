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
    }
}
