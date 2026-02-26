using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace Quintilink.Models
{
    public class StorageModel
    {
        public List<MessageDto> PredefinedMessages { get; set; } = new();

        /// <summary>
        /// Legacy dictionary format for backward compatibility during load.
        /// </summary>
        public Dictionary<string, MessageDto>? Reactions { get; set; }

        /// <summary>
        /// New list-based reactions allowing multiple responses per trigger.
        /// </summary>
        public List<ReactionDto> ReactionsList { get; set; } = new();
    }

    public class MessageDto
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int DelayMs { get; set; } = 0;
        public bool IsPaused { get; set; }
        public int Priority { get; set; }
        public bool StopAfterMatch { get; set; }

        public MessageDto() { }

        public MessageDto(MessageDefinition def)
        {
            Name = def.Name;
            Content = def.Content;
            DelayMs = def.DelayMs;
            IsPaused = def.IsPaused;
            Priority = def.Priority;
            StopAfterMatch = def.StopAfterMatch;
        }

        public MessageDefinition ToDefinition()
            => new MessageDefinition(Name, Content, DelayMs)
            {
                IsPaused = IsPaused,
                Priority = Priority,
                StopAfterMatch = StopAfterMatch
            };
    }

    /// <summary>
    /// DTO for a reaction that includes trigger and response data.
    /// </summary>
    public class ReactionDto
    {
        public string Trigger { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int DelayMs { get; set; } = 0;
        public bool IsPaused { get; set; }
        public int Priority { get; set; }
        public bool StopAfterMatch { get; set; }

        public ReactionDto() { }

        public ReactionDto(string trigger, MessageDefinition def)
        {
            Trigger = trigger;
            Name = def.Name;
            Content = def.Content;
            DelayMs = def.DelayMs;
            IsPaused = def.IsPaused;
            Priority = def.Priority;
            StopAfterMatch = def.StopAfterMatch;
        }

        public (string Trigger, MessageDefinition Definition) ToReaction()
            => (Trigger, new MessageDefinition(Name, Content, DelayMs)
            {
                IsPaused = IsPaused,
                Priority = Priority,
                StopAfterMatch = StopAfterMatch
            });
    }

    /// <summary>
    /// Wrapper class for reactions that WPF can bind to (ValueTuples don't expose named properties).
    /// </summary>
    public class ReactionItem
    {
        public string Trigger { get; set; } = string.Empty;
        public MessageDefinition Response { get; set; } = new();

        public ReactionItem() { }

        public ReactionItem(string trigger, MessageDefinition response)
        {
            Trigger = trigger;
            Response = response;
        }
    }

    public static class MessageStore
    {
        private static readonly string FilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "store.json");

        public static void Save(StorageModel model)
        {
            // Clear legacy dictionary on save so only new format is written
            model.Reactions = null;

            var json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }

        public static StorageModel Load()
        {
            if (!File.Exists(FilePath)) return new StorageModel();

            try
            {
                var json = File.ReadAllText(FilePath);
                var model = JsonSerializer.Deserialize<StorageModel>(json) ?? new StorageModel();

                // Migrate legacy dictionary to list if present
                if (model.Reactions != null && model.Reactions.Count > 0 && model.ReactionsList.Count == 0)
                {
                    foreach (var kv in model.Reactions)
                    {
                        model.ReactionsList.Add(new ReactionDto
                        {
                            Trigger = kv.Key,
                            Name = kv.Value.Name,
                            Content = kv.Value.Content,
                            DelayMs = kv.Value.DelayMs,
                            IsPaused = kv.Value.IsPaused
                        });
                    }
                }

                return model;
            }
            catch
            {
                // fallback if corrupted
                return new StorageModel();
            }
        }
    }
}
