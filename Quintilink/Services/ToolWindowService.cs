using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Quintilink.Models;
using Quintilink.ViewModels;
using Quintilink.Views;

namespace Quintilink.Services
{
    public class ToolWindowService : IToolWindowService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IWindowService _windowService;
        private readonly IDispatcherService _dispatcherService;
        private StatisticsWindow? _statisticsWindow;
        private System.Threading.Timer? _statisticsWindowUpdateTimer;

        public ToolWindowService(IServiceProvider serviceProvider, IWindowService windowService, IDispatcherService dispatcherService)
        {
            _serviceProvider = serviceProvider;
            _windowService = windowService;
            _dispatcherService = dispatcherService;
        }

        public void ShowStatistics()
        {
            _dispatcherService.Invoke(() =>
            {
                if (_statisticsWindow == null)
                {
                    var vm = _serviceProvider.GetRequiredService<StatisticsViewModel>();
                    _statisticsWindow = _serviceProvider.GetRequiredService<StatisticsWindow>();
                    PrepareWindow(_statisticsWindow, vm);

                    _statisticsWindow.Closed += (_, __) =>
                    {
                        _statisticsWindowUpdateTimer?.Dispose();
                        _statisticsWindowUpdateTimer = null;
                        _statisticsWindow = null;
                    };

                    _statisticsWindow.Show();

                    _statisticsWindowUpdateTimer?.Dispose();
                    _statisticsWindowUpdateTimer = new System.Threading.Timer(_ =>
                    {
                        _dispatcherService.Invoke(() =>
                        {
                            if (_statisticsWindow?.DataContext is StatisticsViewModel svm)
                            {
                                svm.UpdateStatistics();
                            }
                        });
                    }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(250));

                    return;
                }

                if (!_statisticsWindow.IsVisible)
                {
                    _statisticsWindow.Show();
                }

                _statisticsWindow.Activate();
            });
        }

        public void ShowAbout()
        {
            _dispatcherService.Invoke(() =>
            {
                var vm = _serviceProvider.GetRequiredService<AboutViewModel>();
                var window = _serviceProvider.GetRequiredService<AboutWindow>();
                PrepareWindow(window, vm);
                window.ShowDialog();
            });
        }

        public void ShowSearchResults(string pattern, List<LogEntry> matches)
        {
            _dispatcherService.Invoke(() =>
            {
                var vm = _serviceProvider.GetRequiredService<SearchResultsViewModel>();
                vm.LoadResults(pattern, matches);

                var window = _serviceProvider.GetRequiredService<SearchResultsWindow>();
                PrepareWindow(window, vm);
                window.Show();
                window.Activate();
            });
        }

        public void ShowHexComparison(IEnumerable<MessageDefinition> messages)
        {
            _dispatcherService.Invoke(() =>
            {
                var vm = _serviceProvider.GetRequiredService<HexComparisonViewModel>();
                vm.LoadMessages(messages);

                var window = _serviceProvider.GetRequiredService<HexComparisonWindow>();
                PrepareWindow(window, vm);
                window.Show();
                window.Activate();
            });
        }

        private void PrepareWindow(Window window, object viewModel)
        {
            window.DataContext = viewModel;
            window.Owner = _windowService.MainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
    }
}
