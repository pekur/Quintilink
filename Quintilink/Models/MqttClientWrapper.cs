using MQTTnet;
using MQTTnet.Protocol;
using Quintilink.Services;
using System.Buffers;

namespace Quintilink.Models
{
    public class MqttClientWrapper : IMqttClientConnection
    {
        private readonly MqttClientFactory _factory = new();
        private IMqttClient? _client;
        private bool _intentionalDisconnect;

        public event Action<string, byte[]>? MessageReceived;
        public event Action<bool>? Disconnected;
        public event Action? Connected;

        public bool IsConnected => _client?.IsConnected ?? false;

        public async Task ConnectAsync(MqttConnectionOptions options, CancellationToken cancellationToken = default)
        {
            DisposeClient();

            _client = _factory.CreateMqttClient();
            _intentionalDisconnect = false;

            _client.ApplicationMessageReceivedAsync += e =>
            {
                byte[] payload = e.ApplicationMessage.Payload.ToArray();
                MessageReceived?.Invoke(e.ApplicationMessage.Topic ?? string.Empty, payload);
                return Task.CompletedTask;
            };

            _client.DisconnectedAsync += _ =>
            {
                Disconnected?.Invoke(!_intentionalDisconnect);
                return Task.CompletedTask;
            };

            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(options.Host, options.Port)
                .WithCleanSession(options.CleanSession)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(options.KeepAliveSeconds <= 0 ? 15 : options.KeepAliveSeconds));

            builder = !string.IsNullOrWhiteSpace(options.ClientId)
                ? builder.WithClientId(options.ClientId)
                : builder.WithClientId("Quintilink_" + Guid.NewGuid().ToString("N").Substring(0, 8));

            if (!string.IsNullOrEmpty(options.Username))
                builder = builder.WithCredentials(options.Username, options.Password ?? string.Empty);

            if (options.UseTls)
                builder = builder.WithTlsOptions(o => o.UseTls(true));

            var mqttOptions = builder.Build();

            var result = await _client.ConnectAsync(mqttOptions, cancellationToken);
            if (result.ResultCode != MqttClientConnectResultCode.Success)
                throw new InvalidOperationException($"MQTT connection rejected: {result.ResultCode}");

            Connected?.Invoke();
        }

        public async Task<bool> SubscribeAsync(string topic, int qos)
        {
            if (_client?.IsConnected != true || string.IsNullOrWhiteSpace(topic))
                return false;

            try
            {
                await _client.SubscribeAsync(topic, ToQos(qos));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UnsubscribeAsync(string topic)
        {
            if (_client?.IsConnected != true || string.IsNullOrWhiteSpace(topic))
                return false;

            try
            {
                await _client.UnsubscribeAsync(topic);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> PublishAsync(string topic, byte[] payload, int qos, bool retain)
        {
            if (_client?.IsConnected != true || string.IsNullOrWhiteSpace(topic))
                return false;

            try
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload ?? Array.Empty<byte>())
                    .WithQualityOfServiceLevel(ToQos(qos))
                    .WithRetainFlag(retain)
                    .Build();

                await _client.PublishAsync(message);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Disconnect()
        {
            _intentionalDisconnect = true;

            try
            {
                _ = _client?.DisconnectAsync();
            }
            catch
            {
                // ignore disconnect errors
            }
        }

        private void DisposeClient()
        {
            if (_client is null)
                return;

            try
            {
                _client.Dispose();
            }
            catch
            {
                // ignore dispose errors
            }
            finally
            {
                _client = null;
            }
        }

        private static MqttQualityOfServiceLevel ToQos(int qos) => qos switch
        {
            1 => MqttQualityOfServiceLevel.AtLeastOnce,
            2 => MqttQualityOfServiceLevel.ExactlyOnce,
            _ => MqttQualityOfServiceLevel.AtMostOnce
        };
    }
}
