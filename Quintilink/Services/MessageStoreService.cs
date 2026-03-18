using Quintilink.Models;

namespace Quintilink.Services
{
    public class MessageStoreService : IMessageStoreService
    {
        public StorageModel Load()
        {
            return MessageStore.Load();
        }

        public void Save(StorageModel model)
        {
            MessageStore.Save(model);
        }
    }
}
