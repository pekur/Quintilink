using System.Windows;

namespace Quintilink.Services
{
    public interface IWindowService
    {
        /// <summary>
        /// Gets the main application window
        /// </summary>
        Window? MainWindow { get; }
    }
}
