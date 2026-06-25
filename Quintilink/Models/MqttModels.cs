using CommunityToolkit.Mvvm.ComponentModel;

namespace Quintilink.Models
{
    /// <summary>
    /// A predefined MQTT topic + payload that can be published to the broker on demand.
    /// </summary>
    public class MqttPublishDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public int Qos { get; set; }
        public bool Retain { get; set; }

        public string PayloadPreview =>
            string.IsNullOrEmpty(Payload) ? "(empty)" : Payload;

        public string QosRetainSummary =>
            $"QoS {Qos}{(Retain ? " · retain" : string.Empty)}";
    }

    /// <summary>
    /// A topic subscription managed in the UI. Tracks whether it is currently active.
    /// </summary>
    public partial class MqttSubscriptionItem : ObservableObject
    {
        [ObservableProperty]
        private string topic = string.Empty;

        [ObservableProperty]
        private int qos;

        [ObservableProperty]
        private bool isSubscribed;
    }

    /// <summary>Persisted form of a predefined MQTT publish message.</summary>
    public class MqttPublishDto
    {
        public string Name { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public int Qos { get; set; }
        public bool Retain { get; set; }

        public MqttPublishDto() { }

        public MqttPublishDto(MqttPublishDefinition def)
        {
            Name = def.Name;
            Topic = def.Topic;
            Payload = def.Payload;
            Qos = def.Qos;
            Retain = def.Retain;
        }

        public MqttPublishDefinition ToDefinition() => new()
        {
            Name = Name,
            Topic = Topic,
            Payload = Payload,
            Qos = Qos,
            Retain = Retain
        };
    }

    /// <summary>Persisted form of an MQTT subscription.</summary>
    public class MqttSubscriptionDto
    {
        public string Topic { get; set; } = string.Empty;
        public int Qos { get; set; }

        public MqttSubscriptionDto() { }

        public MqttSubscriptionDto(MqttSubscriptionItem item)
        {
            Topic = item.Topic;
            Qos = item.Qos;
        }

        public MqttSubscriptionItem ToItem() => new()
        {
            Topic = Topic,
            Qos = Qos
        };
    }
}
