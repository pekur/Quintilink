using System;
using System.Threading.Tasks;
using System.Windows;
using Quintilink.ViewModels;
using Quintilink.Views;

namespace Quintilink.Services
{
    public class DialogService : IDialogService
    {
        private readonly IWindowService _windowService;

        public DialogService(IWindowService windowService)
        {
            _windowService = windowService;
        }

        public async Task<bool?> ShowDialogAsync<TViewModel>(TViewModel viewModel) where TViewModel : class
        {
            var tcs = new TaskCompletionSource<bool?>();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Window? dialog = CreateDialogForViewModel(viewModel);

                if (dialog != null)
                {
                    dialog.DataContext = viewModel;
                    dialog.Owner = _windowService.MainWindow;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    // Subscribe to RequestClose event if ViewModel has it
                    if (viewModel is MessageEditorViewModel messageEditor)
                    {
                        messageEditor.RequestClose += (dialogResult) =>
                        {
                            dialog.DialogResult = dialogResult;
                        };
                    }
                    else if (viewModel is ResponseEditorViewModel responseEditor)
                    {
                        responseEditor.RequestClose += (dialogResult) =>
                        {
                            dialog.DialogResult = dialogResult;
                        };
                    }

                    // Complete task when dialog closes
                    dialog.Closed += (s, e) =>
                    {
                        tcs.SetResult(dialog.DialogResult);
                    };

                    // Show dialog non-blocking
                    dialog.Show();
                }
                else
                {
                    tcs.SetResult(null);
                }
            });

            return await tcs.Task;
        }

        public void ShowMessage(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private Window? CreateDialogForViewModel<TViewModel>(TViewModel viewModel) where TViewModel : class
        {
            return viewModel switch
            {
                MessageEditorViewModel => new MessageEditorWindow(),
                ResponseEditorViewModel => new ResponseEditorWindow(),
                _ => null
            };
        }
    }
}
