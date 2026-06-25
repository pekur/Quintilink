namespace Quintilink.Models
{
    /// <summary>
    /// Settings used to establish an MQTT client connection to a broker.
    /// </summary>
    public class MqttConnectionOptions
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 1883;
        public string ClientId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int KeepAliveSeconds { get; set; } = 15;
        public bool CleanSession { get; set; } = true;
        public bool UseTls { get; set; }
    }
}
