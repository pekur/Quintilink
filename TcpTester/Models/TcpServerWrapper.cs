using System.Net;
using System.Net.Sockets;

namespace TcpTester.Models
{
    public class TcpServerWrapper
    {
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private readonly List<TcpClient> _clients = new();

        public event Action<string, byte[]>? DataReceived;
        public event Action<string>? ClientConnected;
        public event Action<string>? ClientDisconnected;

        public async Task StartAsync(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _cts = new CancellationTokenSource();

            _ = Task.Run(() => AcceptLoop(_cts.Token));
        }

        private async Task AcceptLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _listener is not null)
            {
                var client = await _listener.AcceptTcpClientAsync(ct);

                string endpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
                ClientConnected?.Invoke(endpoint);

                lock (_clients) _clients.Add(client);
                _ = Task.Run(() => HandleClient(client, endpoint, ct));
            }
        }

        private async Task HandleClient(TcpClient client, string endpoint, CancellationToken ct)
        {
            try
            {
                using var stream = client.GetStream();
                var buffer = new byte[4096];
                while (!ct.IsCancellationRequested)
                {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (read == 0) break;

                    DataReceived?.Invoke(endpoint, buffer.Take(read).ToArray());
                }
            }
            catch
            {
                // ignore errors
            }
            finally
            {
                lock (_clients) _clients.Remove(client);
                ClientDisconnected?.Invoke(endpoint);
            }
        }

        public async Task<bool> SendAsync(byte[] data)
        {
            List<TcpClient> clientsCopy;
            lock (_clients) clientsCopy = _clients.ToList();

            bool anySuccess = false;

            foreach (var client in clientsCopy)
            {
                if (client.Connected)
                {
                    try
                    {
                        await client.GetStream().WriteAsync(data, 0, data.Length);
                        anySuccess = true;
                    }
                    catch
                    {
                        // ignore broken clients
                    }
                }
            }

            return anySuccess;
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            lock (_clients)
            {
                foreach (var c in _clients) c.Close();
                _clients.Clear();
            }
        }
    }
}
