using Quintilink.Models;

namespace Quintilink.Services
{
    public class AppSettingsService : IAppSettingsService
    {
        public AppSettings Current { get; } = AppSettings.Load();

        public void Save()
        {
            Current.Save();
        }
    }
}
