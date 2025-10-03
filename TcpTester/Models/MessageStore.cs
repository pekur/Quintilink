using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace TcpTester.Models
{
    public class StorageModel
    {
        public List<MessageDto> PredefinedMessages { get; set; } = new();
        public Dictionary<string, MessageDto> Reactions { get; set; } = new();
    }

    public class MessageDto
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int DelayMs { get; set; } = 0;

        public MessageDto() { }

        public MessageDto(MessageDefinition def)
        {
            Name = def.Name;
            Content = def.Content;
            DelayMs = def.DelayMs;
        }

        public MessageDefinition ToDefinition()
            => new MessageDefinition(Name, Content, DelayMs);
    }

    public static class MessageStore
    {
        private static readonly string FilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "store.json");

        public static void Save(StorageModel model)
        {
            var json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }

        public static StorageModel Load()
        {
            if (!File.Exists(FilePath)) return new StorageModel();

            try
            {
                var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<StorageModel>(json) ?? new StorageModel();
            }
            catch
            {
                // fallback if corrupted
                return new StorageModel();
            }
        }
    }
}
