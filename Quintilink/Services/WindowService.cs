using System.Windows;

namespace Quintilink.Services
{
    public class WindowService : IWindowService
    {
 public Window? MainWindow => Application.Current?.MainWindow;
    }
}
