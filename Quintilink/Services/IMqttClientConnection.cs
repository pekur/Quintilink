using Quintilink.Models;

namespace Quintilink.Services
{
    public interface IMqttClientConnection
    {
        /// <summary>Raised when an application message is received (topic, payload).</summary>
        event Action<string, byte[]>? MessageReceived;

        /// <summary>Raised when the connection drops. Argument is true when the broker/network caused it.</summary>
        event Action<bool>? Disconnected;

        /// <summary>Raised when the client successfully connects to the broker.</summary>
        event Action? Connected;

        bool IsConnected { get; }

        Task ConnectAsync(MqttConnectionOptions options, CancellationToken cancellationToken = default);
        Task<bool> SubscribeAsync(string topic, int qos);
        Task<bool> UnsubscribeAsync(string topic);
        Task<bool> PublishAsync(string topic, byte[] payload, int qos, bool retain);
        void Disconnect();
    }
}
