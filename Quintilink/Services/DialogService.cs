using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Quintilink.ViewModels;
using Quintilink.Views;

namespace Quintilink.Services
{
    public class DialogService : IDialogService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDialogWindowFactory _dialogWindowFactory;
        private readonly IWindowService _windowService;
        private readonly IDispatcherService _dispatcherService;

        public DialogService(
            IServiceProvider serviceProvider,
            IDialogWindowFactory dialogWindowFactory,
            IWindowService windowService,
            IDispatcherService dispatcherService)
        {
            _serviceProvider = serviceProvider;
            _dialogWindowFactory = dialogWindowFactory;
            _windowService = windowService;
            _dispatcherService = dispatcherService;
        }

        public TViewModel CreateViewModel<TViewModel>() where TViewModel : class
        {
            return _serviceProvider.GetRequiredService<TViewModel>();
        }

        public async Task<bool?> ShowDialogAsync<TViewModel>(TViewModel viewModel) where TViewModel : class
        {
            var tcs = new TaskCompletionSource<bool?>();

            await _dispatcherService.InvokeAsync(() =>
            {
                var dialog = _dialogWindowFactory.CreateWindowFor(typeof(TViewModel));
                if (dialog == null)
                {
                    tcs.TrySetResult(null);
                    return;
                }

                dialog.DataContext = viewModel;
                dialog.Owner = _windowService.MainWindow;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                Action<bool>? closeHandler = null;
                if (viewModel is IDialogRequestClose requestClose)
                {
                    closeHandler = dialogResult => dialog.DialogResult = dialogResult;
                    requestClose.RequestClose += closeHandler;
                }

                dialog.Closed += (_, _) =>
                {
                    if (viewModel is IDialogRequestClose requestClose && closeHandler != null)
                    {
                        requestClose.RequestClose -= closeHandler;
                    }

                    tcs.TrySetResult(dialog.DialogResult);
                };

                dialog.ShowDialog();
            });

            return await tcs.Task;
        }

        public void ShowMessage(string title, string message)
        {
            _dispatcherService.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
    }
}
