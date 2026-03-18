using Quintilink.Models;

namespace Quintilink.Services
{
    public interface IAppSettingsService
    {
        AppSettings Current { get; }

        void Save();
    }
}
