using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TcpTester.ViewModels;
using Wpf.Ui.Controls;

namespace TcpTester.Views
{
    /// <summary>
    /// Interaction logic for ResponseEditorWindow.xaml
    /// </summary>
    public partial class ResponseEditorWindow : FluentWindow
    {
        public ResponseEditorWindow()
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
