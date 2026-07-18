using System.Windows.Controls;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace WinCleaner.Views
{
    public partial class RuntimeInstallerView : UserControl
    {
        public RuntimeInstallerView()
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
