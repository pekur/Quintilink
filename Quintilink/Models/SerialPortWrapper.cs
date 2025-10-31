using System.IO.Ports;

namespace Quintilink.Models
{
    public class SerialPortWrapper
    {
        private SerialPort? _port;
        private bool _isDisconnected;

        public event Action<byte[]>? DataReceived;
        public event Action<bool>? Disconnected;
        public event Action? ModemLinesChanged;

        private void RaiseDisconnected(bool remote)
        {
            if (_isDisconnected) return;
            _isDisconnected = true;
            Disconnected?.Invoke(remote);
        }

        public Task ConnectAsync(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            return Task.Run(() =>
            {
                _port = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
                {
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };

                _port.DataReceived += OnDataReceived;
                _port.ErrorReceived += OnErrorReceived;
                _port.PinChanged += OnPinChanged;

                _port.Open();
                _isDisconnected = false;

                // Notify initial modem line states
                ModemLinesChanged?.Invoke();
            });
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_port == null || !_port.IsOpen) return;

            try
            {
                int bytesToRead = _port.BytesToRead;
                if (bytesToRead > 0)
                {
                    byte[] buffer = new byte[bytesToRead];
                    int read = _port.Read(buffer, 0, bytesToRead);
                    if (read > 0)
                    {
                        DataReceived?.Invoke(buffer.Take(read).ToArray());
                    }
                }
            }
            catch
            {
                RaiseDisconnected(true);
            }
        }

        private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            RaiseDisconnected(true);
        }

        private void OnPinChanged(object sender, SerialPinChangedEventArgs e)
        {
            ModemLinesChanged?.Invoke();
        }

        public Task<bool> SendAsync(byte[] data)
        {
            return Task.Run(() =>
            {
                if (_port?.IsOpen == true)
                {
                    try
                    {
                        _port.Write(data, 0, data.Length);
                        return true;
                    }
                    catch
                    {
                        RaiseDisconnected(true);
                        return false;
                    }
                }

                return false;
            });
        }

        // Modem line status properties
        public bool CtsHolding => _port?.CtsHolding ?? false;
        public bool DsrHolding => _port?.DsrHolding ?? false;
        public bool CDHolding => _port?.CDHolding ?? false;
        // Note: RI (Ring Indicator) is not reliably accessible via .NET SerialPort API
        // It can only be detected via PinChanged event, but state cannot be directly read
        public bool RingIndicator => false; // Not supported in .NET SerialPort

        // DTR and RTS control methods
        public void SetDtrEnable(bool enable)
        {
            if (_port?.IsOpen == true)
            {
                _port.DtrEnable = enable;
            }
        }

        public void SetRtsEnable(bool enable)
        {
            if (_port?.IsOpen == true)
            {
                _port.RtsEnable = enable;
            }
        }

        public bool GetDtrEnable() => _port?.DtrEnable ?? false;
        public bool GetRtsEnable() => _port?.RtsEnable ?? false;

        public void Disconnect()
        {
            if (_port != null)
            {
                _port.DataReceived -= OnDataReceived;
                _port.ErrorReceived -= OnErrorReceived;
                _port.PinChanged -= OnPinChanged;

                if (_port.IsOpen)
                {
                    _port.Close();
                }

                _port.Dispose();
                _port = null;
            }

            RaiseDisconnected(false);
        }

        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }
    }
}
