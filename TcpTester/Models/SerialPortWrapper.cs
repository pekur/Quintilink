using System.IO.Ports;

namespace TcpTester.Models
{
    public class SerialPortWrapper
    {
        private SerialPort? _port;
        private bool _isDisconnected;

        public event Action<byte[]>? DataReceived;
        public event Action<bool>? Disconnected;

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

   _port.Open();
    _isDisconnected = false;
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

        public void Disconnect()
        {
 if (_port != null)
        {
             _port.DataReceived -= OnDataReceived;
       _port.ErrorReceived -= OnErrorReceived;
     
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
