using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Quintilink.Models;

namespace Quintilink.ViewModels
{
    public partial class SearchResultsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string searchPattern = string.Empty;

        [ObservableProperty]
        private int totalResults;

        public ObservableCollection<LogEntry> Results { get; } = new();

        public void LoadResults(string pattern, List<LogEntry> results)
        {
            SearchPattern = pattern;
            TotalResults = results.Count;
            
            Results.Clear();
            foreach (var result in results)
            {
                Results.Add(result);
            }
        }
    }
}
