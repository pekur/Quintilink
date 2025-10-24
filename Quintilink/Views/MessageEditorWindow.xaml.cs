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
            Loaded += (_, __) =>
            {
                if (DataContext is MessageEditorViewModel vm)
                {
                    vm.RequestClose += result =>
                    {
                        DialogResult = result;
                        Close();
                    };
                }
            };
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
            // Accept only 0–9, A–F, a–f, and space
            if (!Regex.IsMatch(e.Text, "^[0-9A-Fa-f ]$"))
            {
                e.Handled = true; // block invalid
            }
            // ✅ For valid chars, leave e.Handled = false → WPF inserts it
        }

        private void HexBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Allow navigation/editing keys
            if (e.Key == Key.Back || e.Key == Key.Delete ||
                e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Tab)
            {
                e.Handled = false;
            }
        }
    }
}
