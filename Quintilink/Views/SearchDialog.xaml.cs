using System.Windows;
using Wpf.Ui.Controls;
using Quintilink.ViewModels;

namespace Quintilink.Views
{
    public partial class SearchDialog : FluentWindow
    {
        public SearchDialog()
        {
            InitializeComponent();
            Loaded += SearchDialog_Loaded;
        }

        private void SearchDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Focus the search pattern textbox when dialog opens
            SearchPatternTextBox.Focus();
        }
    }
}
