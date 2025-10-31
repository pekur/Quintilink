using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Quintilink.ViewModels;
using Wpf.Ui.Controls;

namespace Quintilink.Views
{
    public partial class MessageEditorWindow : FluentWindow
    {
        public MessageEditorWindow()
        {
            InitializeComponent();
        }

        private void HexBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is MessageEditorViewModel vm)
            {
                vm.NormalizeHexField();
            }
        }

        private void HexBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Accept only hexadecimal characters (0-9, A-F, a-f) and spaces
            if (!Regex.IsMatch(e.Text, "^[0-9A-Fa-f ]$"))
            {
                e.Handled = true;
            }
        }

        private void HexBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Allow navigation and editing keys
            if (e.Key == Key.Back || e.Key == Key.Delete ||
                e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Tab)
            {
                e.Handled = false;
            }
        }
    }
}
