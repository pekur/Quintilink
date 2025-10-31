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
        public MainWindow()
        {
            InitializeComponent();
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
