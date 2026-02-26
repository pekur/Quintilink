using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Quintilink.ViewModels;
using Wpf.Ui.Controls;

namespace Quintilink.Views
{
    /// <summary>
    /// Interaction logic for ResponseEditorWindow.xaml
    /// </summary>
    public partial class ResponseEditorWindow : FluentWindow
    {
        public ResponseEditorWindow()
        {
            InitializeComponent();
        }

        private void HexBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is ResponseEditorViewModel vm)
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

        private void NumericBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Regex.IsMatch(e.Text, "^[0-9]+$"))
            {
                e.Handled = true;
            }
        }

        private void NumericBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }
    }
}
