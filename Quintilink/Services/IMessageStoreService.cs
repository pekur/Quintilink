using Quintilink.Models;

namespace Quintilink.Services
{
    public interface IMessageStoreService
    {
        StorageModel Load();

        void Save(StorageModel model);
    }
}
