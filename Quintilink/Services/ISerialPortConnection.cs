using System.IO.Ports;

namespace Quintilink.Services
{
    public interface ISerialPortConnection
    {
        event Action<byte[]>? DataReceived;
        event Action<bool>? Disconnected;
        event Action? ModemLinesChanged;

        Task ConnectAsync(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits);
        Task<bool> SendAsync(byte[] data);
        void Disconnect();
        string[] GetAvailablePorts();
        bool CtsHolding { get; }
        bool DsrHolding { get; }
        bool CDHolding { get; }
        bool RingIndicator { get; }
        void SetDtrEnable(bool enable);
        void SetRtsEnable(bool enable);
        bool GetDtrEnable();
        bool GetRtsEnable();
    }
}
