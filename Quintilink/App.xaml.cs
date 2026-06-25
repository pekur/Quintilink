using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Quintilink.Models;
using Quintilink.Services;
using Quintilink.ViewModels;
using Quintilink.Views;

namespace Quintilink
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configure services
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Create and show main window with injected ViewModel
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register services
            services.AddSingleton<IDispatcherService, DispatcherService>();
            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<ILogExportService, LogExportService>();
            services.AddSingleton<IAppSettingsService, AppSettingsService>();
            services.AddSingleton<IMessageStoreService, MessageStoreService>();
            services.AddSingleton<ITcpClientConnection, TcpClientWrapper>();
            services.AddSingleton<ITcpServerConnection, TcpServerWrapper>();
            services.AddSingleton<ISerialPortConnection, SerialPortWrapper>();
            services.AddSingleton<IMqttClientConnection, MqttClientWrapper>();
            services.AddSingleton<ConnectionStatistics>();
            services.AddSingleton<IHexComparisonService, HexComparisonService>();
            services.AddSingleton<IDialogWindowFactory, DialogWindowFactory>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IToolWindowService, ToolWindowService>();

            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainViewModel>();

            services.AddTransient<MessageEditorViewModel>();
            services.AddTransient<ResponseEditorViewModel>();
            services.AddTransient<MqttPublishEditorViewModel>();
            services.AddTransient<SearchDialogViewModel>();
            services.AddTransient<SearchResultsViewModel>();
            services.AddTransient<AboutViewModel>();
            services.AddTransient<StatisticsViewModel>();
            services.AddTransient<HexComparisonViewModel>();

            services.AddTransient<MessageEditorWindow>();
            services.AddTransient<ResponseEditorWindow>();
            services.AddTransient<MqttPublishEditorWindow>();
            services.AddTransient<SearchDialog>();
            services.AddTransient<SearchResultsWindow>();
            services.AddTransient<AboutWindow>();
            services.AddTransient<StatisticsWindow>();
            services.AddTransient<HexComparisonWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
