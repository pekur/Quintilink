using Quintilink.Models;

namespace Quintilink.Services
{
    public interface IToolWindowService
    {
        void ShowStatistics();
        void ShowAbout();
        void ShowSearchResults(string pattern, List<LogEntry> matches);
        void ShowHexComparison(IEnumerable<MessageDefinition> messages);
    }
}
