namespace Quintilink.Services
{
    public interface ITcpClientConnection
    {
        event Action<byte[]>? DataReceived;
        event Action<bool>? Disconnected;

        Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default);
        Task<bool> SendAsync(byte[] data);
        void Disconnect();
    }
}
