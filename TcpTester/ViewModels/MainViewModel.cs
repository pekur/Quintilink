using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using TcpTester.Models;

namespace TcpTester.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly TcpClientWrapper _client = new();
        private readonly TcpServerWrapper _server = new();
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
        bool isServerMode = false;
        [ObservableProperty]
        private string log = string.Empty;

        [ObservableProperty]
        private bool isConnected;

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
                string ascii = Encoding.ASCII.GetString(data);
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

            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
        }

        private void UpdateServerStatus()
        {
            if (!IsServerMode)
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
                if (IsServerMode)
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
            if (IsServerMode)
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

            if (IsServerMode)
                success = await _server.SendAsync(bytes);
            else
                success = await _client.SendAsync(bytes);

            if (success)
            {
                AppendLog($"[TX] HEX: {MessageDefinition.ToSpacedHex(bytes)} – {bytes.Length} bytes");
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
            string ascii = Encoding.ASCII.GetString(data).Trim();
            string hex = BitConverter.ToString(data).Replace("-", " ");

            AppendLog($"[RX] ASCII: {ascii}");
            AppendLog($"[RX] HEX  : {hex}");

            await CheckReaction(hex);
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

            if (IsServerMode)
                await _server.SendAsync(bytes);
            else
                await _client.SendAsync(bytes);
        }

        private void AppendLog(string entry)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Log += $"[{timestamp}] {entry}{Environment.NewLine}";
            });
        }
    }
}
