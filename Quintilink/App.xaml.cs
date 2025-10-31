using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
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
            var mainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainViewModel>()
            };
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register services
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IDispatcherService, DispatcherService>();
            services.AddSingleton<IWindowService, WindowService>();

            // Register ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<MessageEditorViewModel>();
            services.AddTransient<ResponseEditorViewModel>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
