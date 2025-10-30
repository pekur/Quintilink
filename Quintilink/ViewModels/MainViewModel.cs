using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using Quintilink.Models;
using Quintilink.Helpers;

namespace Quintilink.ViewModels
{
    public enum ConnectionMode
    {
        TcpClient,
        TcpServer,
        SerialPort
    }

    public partial class MainViewModel : ObservableObject
    {
        private readonly TcpClientWrapper _client = new();
        private readonly TcpServerWrapper _server = new();
        private readonly SerialPortWrapper _serialPort = new();
        private readonly AppSettings _settings;

        [ObservableProperty]
        private string host;

        [ObservableProperty]
        private string serverStatus = "Disconnected";
        [ObservableProperty]
        private string serverStatusTooltip = "";

        private readonly List<string> _connectedClients = new();

        partial void OnHostChanged(string value)
        {
            _settings.Host = value;
            _settings.Save();
        }

        [ObservableProperty]
        private int port;

        partial void OnPortChanged(int value)
        {
            _settings.Port = value;
            _settings.Save();
        }

        [ObservableProperty]
        private bool isServerMode = false;

        [ObservableProperty]
        private bool isSerialMode = false;

        [ObservableProperty]
        private FlowDocument logDocument = LogHelper.CreateLogDocument();

        [ObservableProperty]
        private bool isConnected;

        // Serial Port Properties
        [ObservableProperty]
        private string selectedSerialPort;

        partial void OnSelectedSerialPortChanged(string value)
        {
            _settings.SerialPortName = value;
            _settings.Save();
        }

        [ObservableProperty]
        private int selectedBaudRate;

        partial void OnSelectedBaudRateChanged(int value)
        {
            _settings.BaudRate = value;
            _settings.Save();
        }

        [ObservableProperty]
        private Parity selectedParity;

        partial void OnSelectedParityChanged(Parity value)
        {
            _settings.Parity = (int)value;
            _settings.Save();
        }

        [ObservableProperty]
        private int selectedDataBits;

        partial void OnSelectedDataBitsChanged(int value)
        {
            _settings.DataBits = value;
            _settings.Save();
        }

        [ObservableProperty]
        private StopBits selectedStopBits;

        partial void OnSelectedStopBitsChanged(StopBits value)
        {
            _settings.StopBits = (int)value;
            _settings.Save();
        }

        public ObservableCollection<string> AvailableSerialPorts { get; } = new();
        public ObservableCollection<int> BaudRates { get; } = new() { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
        public ObservableCollection<Parity> ParityOptions { get; } = new()
        {
            Parity.None,
            Parity.Odd,
            Parity.Even,
            Parity.Mark,
            Parity.Space
        };
        public ObservableCollection<int> DataBitsOptions { get; } = new() { 5, 6, 7, 8 };
        public ObservableCollection<StopBits> StopBitsOptions { get; } = new()
        {
            StopBits.One,
            StopBits.OnePointFive,
            StopBits.Two
        };

        public ObservableCollection<MessageDefinition> PredefinedMessages { get; } = new();

        private readonly Dictionary<string, MessageDefinition> _reactions = new();

        public ObservableCollection<KeyValuePair<string, MessageDefinition>> Reactions { get; }
            = new ObservableCollection<KeyValuePair<string, MessageDefinition>>();

        [ObservableProperty]
        public string title = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

        public MainViewModel()
        {
            _client.DataReceived += OnDataReceived;
            _client.Disconnected += remote =>
        {
            App.Current.Dispatcher.Invoke(() =>
             {
                 AppendLog(remote
                     ? "[SYS] Disconnected by remote host"
                : "[SYS] Disconnected");

                 IsConnected = false;
                 ConnectCommand.NotifyCanExecuteChanged();
                 DisconnectCommand.NotifyCanExecuteChanged();
                 UpdateServerStatus();
             });
        };

            _server.DataReceived += async (endpoint, data) =>
      {
          string hex = BitConverter.ToString(data).Replace("-", " ");
          string ascii = ConvertToReadableAscii(data);
          App.Current.Dispatcher.Invoke(() =>
          {
              AppendLog($"[RX] {endpoint} : {hex} (ASCII: {ascii})");
          });
          await CheckReaction(hex);
      };

            _server.ClientConnected += endpoint =>
                    {
                        _connectedClients.Add(endpoint);
                        AppendLog($"[SYS] Client connected: {endpoint}");
                        UpdateServerStatus();
                    };

            _server.ClientDisconnected += endpoint =>
            {
                _connectedClients.Remove(endpoint);
                AppendLog($"[SYS] Client disconnected: {endpoint}");
                UpdateServerStatus();
            };

            _serialPort.DataReceived += OnDataReceived;
            _serialPort.Disconnected += remote =>
          {
              App.Current.Dispatcher.Invoke(() =>
          {
              AppendLog(remote
                 ? "[SYS] Serial port disconnected (error)"
                : "[SYS] Serial port disconnected");

              IsConnected = false;
              ConnectCommand.NotifyCanExecuteChanged();
              DisconnectCommand.NotifyCanExecuteChanged();
              UpdateServerStatus();
          });
          };

            // Load messages and reactions
            var loaded = MessageStore.Load();

            PredefinedMessages.Clear();
            foreach (var dto in loaded.PredefinedMessages)
                PredefinedMessages.Add(dto.ToDefinition());

            _reactions.Clear();
            foreach (var kv in loaded.Reactions)
                _reactions[kv.Key] = kv.Value.ToDefinition();

            RefreshReactions();

            // load settings
            _settings = AppSettings.Load();
            Host = _settings.Host;
            Port = _settings.Port;

            // Load serial port settings
            RefreshSerialPorts();
            SelectedSerialPort = _settings.SerialPortName;
            SelectedBaudRate = _settings.BaudRate;
            SelectedParity = (Parity)_settings.Parity;
            SelectedDataBits = _settings.DataBits;
            SelectedStopBits = (StopBits)_settings.StopBits;

            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void RefreshSerialPorts()
        {
            AvailableSerialPorts.Clear();
            foreach (var port in SerialPortWrapper.GetAvailablePorts())
            {
                AvailableSerialPorts.Add(port);
            }
        }

        private void UpdateServerStatus()
        {
            if (IsSerialMode)
            {
                ServerStatus = IsConnected ? $"Connected ({SelectedSerialPort})" : "Disconnected";
                ServerStatusTooltip = IsConnected ? $"{SelectedBaudRate} baud, {SelectedDataBits}{SelectedParity.ToString()[0]}{(int)SelectedStopBits}" : "";
            }
            else if (!IsServerMode)
            {
                ServerStatus = IsConnected ? "Connected" : "Disconnected";
                ServerStatusTooltip = "";
            }
            else
            {
                if (!IsConnected)
                {
                    ServerStatus = "Disconnected";
                    ServerStatusTooltip = "";
                }
                else if (_connectedClients.Count == 0)
                {
                    ServerStatus = "Server - listening";
                    ServerStatusTooltip = $"Listening on port {Port}";
                }
                else if (_connectedClients.Count == 1)
                {
                    ServerStatus = $"Server - 1 client";
                    ServerStatusTooltip = _connectedClients[0];
                }
                else
                {
                    ServerStatus = $"Server - {_connectedClients.Count} clients";
                    ServerStatusTooltip = string.Join(Environment.NewLine, _connectedClients);
                }
            }
        }

        public void SaveMessages()
        {
            var storage = new StorageModel
            {
                PredefinedMessages = PredefinedMessages
                    .Select(m => new MessageDto(m))
                    .ToList(),
                Reactions = _reactions.ToDictionary(
                    kv => kv.Key,
                    kv => new MessageDto(kv.Value))
            };

            MessageStore.Save(storage);
        }

        [RelayCommand(CanExecute = nameof(CanConnect))]
        private async Task ConnectAsync()
        {
            try
            {
                if (IsSerialMode)
                {
                    await _serialPort.ConnectAsync(
                   SelectedSerialPort,
                SelectedBaudRate,
                       SelectedParity,
                        SelectedDataBits,
                       SelectedStopBits);
                    AppendLog($"[SYS] Serial port {SelectedSerialPort} opened at {SelectedBaudRate} baud");
                }
                else if (IsServerMode)
                {
                    await _server.StartAsync(Port);
                    AppendLog($"[SYS] Server started on port {Port}");
                }
                else
                {
                    await _client.ConnectAsync(Host, Port);
                    AppendLog($"[SYS] Connected to {Host}:{Port}");
                }

                IsConnected = true;
            }
            catch (Exception ex)
            {
                AppendLog($"[ERR] Failed to connect: {ex.Message}");
                IsConnected = false;
            }

            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
            UpdateServerStatus();
        }

        [RelayCommand(CanExecute = nameof(CanDisconnect))]
        private void Disconnect()
        {
            if (IsSerialMode)
            {
                _serialPort.Disconnect();
                AppendLog("[SYS] Serial port closed");
            }
            else if (IsServerMode)
            {
                _server.Stop();
                AppendLog("[SYS] Server stopped");
            }
            else
            {
                _client.Disconnect();
            }

            IsConnected = false;
            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
            UpdateServerStatus();
        }

        private bool CanConnect() => !IsConnected;
        private bool CanDisconnect() => IsConnected;

        [RelayCommand]
        private async Task SendMessageAsync(MessageDefinition? def)
        {
            if (def is null) return;

            var bytes = def.GetBytes();
            bool success = false;

            if (IsSerialMode)
                success = await _serialPort.SendAsync(bytes);
            else if (IsServerMode)
                success = await _server.SendAsync(bytes);
            else
                success = await _client.SendAsync(bytes);

            if (success)
            {
                string ascii = ConvertToReadableAscii(bytes);
                string hex = MessageDefinition.ToSpacedHex(bytes);
                AppendLog($"[TX] ASCII: {ascii}");
                AppendLog($"[TX] HEX  : {hex} – {bytes.Length} bytes");
            }
            else
            {
                AppendLog($"[ERR] Failed to send \"{def?.Name ?? "Unknown"}\" – not connected");
            }
        }

        [RelayCommand]
        private void AddMessage()
        {
            var editor = new MessageEditorViewModel();
            var dlg = new Views.MessageEditorWindow
            {
                DataContext = editor,
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (dlg.ShowDialog() == true)
            {
                PredefinedMessages.Add(editor.ToDefinition());
                SaveMessages();
            }
        }


        [RelayCommand]
        private void EditMessage(MessageDefinition? message)
        {
            if (message is null) return;
            var editor = new MessageEditorViewModel(message);
            var dlg = new Views.MessageEditorWindow
            {
                DataContext = editor,
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (dlg.ShowDialog() == true)
            {
                var index = PredefinedMessages.IndexOf(message);
                if (index >= 0)
                {
                    PredefinedMessages[index] = editor.ToDefinition();
                    SaveMessages();
                }
            }
        }

        [RelayCommand]
        private void DeleteMessage(MessageDefinition? message)
        {
            if (message is not null)
            {
                PredefinedMessages.Remove(message);
                SaveMessages();

            }
        }

        [RelayCommand]
        private void AddReaction()
        {
            var vm = new ResponseEditorViewModel();
            var dlg = new Views.ResponseEditorWindow { DataContext = vm, Owner = Application.Current.MainWindow };
            vm.RequestClose += result => dlg.DialogResult = result;

            if (dlg.ShowDialog() == true)
            {
                _reactions[vm.Trigger] = vm.ToDefinition();
                RefreshReactions();
                SaveMessages();
            }
        }

        [RelayCommand]
        private void EditReaction(KeyValuePair<string, MessageDefinition>? item)
        {
            if (item is null) return;

            var vm = new ResponseEditorViewModel(item.Value.Key, item.Value.Value);
            var dlg = new Views.ResponseEditorWindow { DataContext = vm, Owner = Application.Current.MainWindow };
            vm.RequestClose += result => dlg.DialogResult = result;

            if (dlg.ShowDialog() == true)
            {
                _reactions.Remove(item.Value.Key);
                _reactions[vm.Trigger] = vm.ToDefinition();
                RefreshReactions();
                SaveMessages();
            }
        }

        [RelayCommand]
        private void DeleteReaction(KeyValuePair<string, MessageDefinition>? item)
        {
            if (item is null) return;

            if (_reactions.Remove(item.Value.Key))
            {
                RefreshReactions();
                SaveMessages();
            }
        }

        private void RefreshReactions()
        {
            Reactions.Clear();
            foreach (var kv in _reactions)
                Reactions.Add(kv);
        }

        private async void OnDataReceived(byte[] data)
        {
            string ascii = ConvertToReadableAscii(data);
            string hex = BitConverter.ToString(data).Replace("-", " ");

            AppendLog($"[RX] ASCII: {ascii}");
            AppendLog($"[RX] HEX  : {hex}");

            await CheckReaction(hex);
        }

        private static string ConvertToReadableAscii(byte[] data)
        {
            var sb = new StringBuilder();
            foreach (var b in data)
            {
                sb.Append(MacroDefinitions.CollapseByte(b));
            }
            return sb.ToString();
        }

        private async Task CheckReaction(string hex)
        {
            if (_reactions.TryGetValue(hex, out var response))
            {
                await SendReaction(hex, response);
                return;
            }

            foreach (var kv in _reactions)
            {
                var trigger = kv.Key;
                if (hex.StartsWith(trigger, StringComparison.OrdinalIgnoreCase))
                {
                    await SendReaction(trigger, kv.Value);
                    return;
                }
            }
        }

        private async Task SendReaction(string trigger, MessageDefinition response)
        {
            AppendLog($"[SYS] Reaction triggered for '{trigger}' → sending '{response.Content}'");

            if (response.DelayMs > 0)
                await Task.Delay(response.DelayMs);

            var bytes = response.GetBytes();

            if (IsSerialMode)
                await _serialPort.SendAsync(bytes);
            else if (IsServerMode)
                await _server.SendAsync(bytes);
            else
                await _client.SendAsync(bytes);
        }

        private void AppendLog(string entry)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            Application.Current.Dispatcher.Invoke(() =>
                  {
                      // Determine if this is an ASCII line
                      bool isAsciiLine = entry.StartsWith("[RX] ASCII:") || entry.StartsWith("[TX] ASCII:");

                      // Extract prefix and content
                      string prefix = "";
                      string content = entry;

                      if (entry.StartsWith("["))
                      {
                          int endBracket = entry.IndexOf(']');
                          if (endBracket > 0)
                          {
                              prefix = entry.Substring(0, endBracket + 1) + " ";
                              content = entry.Substring(endBracket + 2);
                          }
                      }

                      LogHelper.AppendLogEntry(logDocument, timestamp, prefix, content, isAsciiLine);
                  });
        }
    }
}
