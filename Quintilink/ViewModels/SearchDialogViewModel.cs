using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Quintilink.Models;

namespace Quintilink.ViewModels
{
    public partial class SearchDialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private string searchPattern = string.Empty;

        [ObservableProperty]
        private SearchDirection selectedDirection = SearchDirection.All;

        [ObservableProperty]
        private bool caseSensitive = false;

        [ObservableProperty]
        private bool useRegex = false;

        public ObservableCollection<SearchDirection> Directions { get; } = new()
        {
            SearchDirection.All,
            SearchDirection.TX,
            SearchDirection.RX,
            SearchDirection.SYS,
            SearchDirection.ERR
        };

        public event Action<bool>? RequestClose;

        [RelayCommand(CanExecute = nameof(CanSearch))]
        private void Search()
        {
            RequestClose?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }

        private bool CanSearch() => !string.IsNullOrWhiteSpace(SearchPattern);

        partial void OnSearchPatternChanged(string value)
        {
            SearchCommand.NotifyCanExecuteChanged();
        }
    }
}
