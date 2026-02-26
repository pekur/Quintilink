using System.Net.Sockets;

namespace Quintilink.Models
{
    public class TcpClientWrapper
    {
        private TcpClient? _client;
        private CancellationTokenSource? _cts;
        private bool _isDisconnected;

        public event Action<byte[]>? DataReceived;
        public event Action<bool>? Disconnected;

        private void RaiseDisconnected(bool remote)
        {
            if (_isDisconnected) return;
            _isDisconnected = true;
            Disconnected?.Invoke(remote);
        }

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port, cancellationToken);
            _cts = new CancellationTokenSource();
            _isDisconnected = false;

            _ = Task.Run(() => ReceiveLoop(_cts.Token));
        }

        private async Task ReceiveLoop(CancellationToken ct)
        {
            try
            {
                using var stream = _client!.GetStream();
                var buffer = new byte[4096];

                while (!ct.IsCancellationRequested)
                {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (read == 0)
                    {
                        RaiseDisconnected(true); // remote closed
                        return;
                    }
                    DataReceived?.Invoke(buffer.Take(read).ToArray());
                }
            }
            catch
            {
                RaiseDisconnected(true);
            }
            finally
            {
                _client?.Close();
            }
        }

        public async Task<bool> SendAsync(byte[] data)
        {
            if (_client?.Connected == true)
            {
                try
                {
                    await _client.GetStream().WriteAsync(data, 0, data.Length);
                    return true;
                }
                catch
                {
                    RaiseDisconnected(true);
                    return false;
                }
            }

            return false;
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            _client?.Close();
            RaiseDisconnected(false);
        }
    }
}
