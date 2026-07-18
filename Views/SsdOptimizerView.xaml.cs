using System.Windows.Controls;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace WinCleaner.Views
{
    public partial class SsdOptimizerView : UserControl
    {
        public SsdOptimizerView()
        {
            InitializeComponent();
        }

        private void ConsoleLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is WpfTextBox textBox)
            {
                textBox.ScrollToEnd();
            }
        }
    }
}
