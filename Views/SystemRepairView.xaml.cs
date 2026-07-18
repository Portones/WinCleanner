using System.Windows.Controls;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace WinCleaner.Views
{
    public partial class SystemRepairView : UserControl
    {
        public SystemRepairView()
        {
            InitializeComponent();
        }

        private void ConsoleOutput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is WpfTextBox textBox)
            {
                textBox.ScrollToEnd();
            }
        }
    }
}
