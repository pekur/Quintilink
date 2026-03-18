namespace Quintilink.Services
{
    public interface ITcpServerConnection
    {
        event Action<string, byte[]>? DataReceived;
        event Action<string>? ClientConnected;
        event Action<string>? ClientDisconnected;

        Task StartAsync(int port);
        Task<bool> SendAsync(byte[] data);
        void Stop();
    }
}
