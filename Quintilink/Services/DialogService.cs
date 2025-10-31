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

        public Task<bool?> ShowDialogAsync<TViewModel>(TViewModel viewModel) where TViewModel : class
        {
            bool? result = null;

            // Must run on UI thread synchronously for ShowDialog()
            Application.Current.Dispatcher.Invoke(() =>
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

                    // ShowDialog() is synchronous and blocks until dialog closes
                    result = dialog.ShowDialog();
                }
            });

            return Task.FromResult(result);
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
