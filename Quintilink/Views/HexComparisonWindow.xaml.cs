using System.Windows;
using Wpf.Ui.Controls;

namespace Quintilink.Views
{
    public partial class HexComparisonWindow : FluentWindow
    {
        public HexComparisonWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
