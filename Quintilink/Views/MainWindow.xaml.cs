using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Quintilink.Models;
using Quintilink.ViewModels;
using Wpf.Ui.Controls;

namespace Quintilink.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        private const double BookmarkGutterWidth = 18;
        private AppSettings? _windowSettings;

        public MainWindow()
        {
            InitializeComponent();

            SourceInitialized += MainWindow_SourceInitialized;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            _windowSettings = AppSettings.Load();

            if (_windowSettings.MainWindowWidth is not double width
                || _windowSettings.MainWindowHeight is not double height
                || _windowSettings.MainWindowLeft is not double left
                || _windowSettings.MainWindowTop is not double top
                || width <= 0
                || height <= 0)
            {
                return;
            }

            var targetRect = new Rect(left, top, width, height);
            var virtualScreen = new Rect(
                SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth,
                SystemParameters.VirtualScreenHeight);

            if (!targetRect.IntersectsWith(virtualScreen))
                return;

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = left;
            Top = top;
            Width = width;
            Height = height;

            if (_windowSettings.MainWindowMaximized)
                WindowState = WindowState.Maximized;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _windowSettings ??= AppSettings.Load();

            var bounds = RestoreBounds;
            if (bounds.Width > 0 && bounds.Height > 0)
            {
                _windowSettings.MainWindowLeft = bounds.Left;
                _windowSettings.MainWindowTop = bounds.Top;
                _windowSettings.MainWindowWidth = bounds.Width;
                _windowSettings.MainWindowHeight = bounds.Height;
            }

            _windowSettings.MainWindowMaximized = WindowState == WindowState.Maximized;
            _windowSettings.Save();
        }

        private void LogRichTextBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not System.Windows.Controls.RichTextBox rtb)
                return;

            var pos = e.GetPosition(rtb);
            var desired = pos.X <= BookmarkGutterWidth ? Cursors.Arrow : Cursors.IBeam;

            if (rtb.Cursor != desired)
                rtb.Cursor = desired;
        }

        private void LogRichTextBox_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is System.Windows.Controls.RichTextBox rtb)
                rtb.Cursor = Cursors.IBeam;
        }

        private void LogRichTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not System.Windows.Controls.RichTextBox rtb)
                return;

            var pos = e.GetPosition(rtb);
            if (pos.X > BookmarkGutterWidth)
                return;

            var textPointer = rtb.GetPositionFromPoint(pos, snapToText: true);
            if (textPointer == null)
                return;

            var paragraph = textPointer.Paragraph;
            if (paragraph == null)
                return;

            if (DataContext is not MainViewModel vm)
                return;

            // Find the paragraph index inside the FlowDocument
            int index = 0;
            foreach (var block in rtb.Document.Blocks)
            {
                if (ReferenceEquals(block, paragraph))
                    break;
                index++;
            }

            if (index < 0 || index >= rtb.Document.Blocks.Count)
                return;

            if (vm.ToggleBookmarkAtVisibleIndexCommand.CanExecute(index))
            {
                vm.ToggleBookmarkAtVisibleIndexCommand.Execute(index);
                e.Handled = true;
            }
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.DataContext is MessageDefinition msg)
            {
                if (DataContext is MainViewModel vm && vm.SendMessageCommand.CanExecute(msg))
                {
                    vm.SendMessageCommand.Execute(msg);
                }
            }
        }

    }
}
