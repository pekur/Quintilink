namespace Quintilink.Services
{
    public interface IDialogService
    {
        /// <summary>
   /// Shows a dialog window with the specified ViewModel and returns a result
        /// </summary>
        Task<bool?> ShowDialogAsync<TViewModel>(TViewModel viewModel) where TViewModel : class;

        /// <summary>
   /// Shows a message box
        /// </summary>
        void ShowMessage(string title, string message);
    }
}
