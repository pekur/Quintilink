using System.Windows;
using Wpf.Ui.Controls;

namespace Quintilink.Views
{
    public partial class SearchResultsWindow : FluentWindow
    {
        public SearchResultsWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
