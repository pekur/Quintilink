using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using Quintilink.Models;
using Quintilink.Helpers;
using Quintilink.Services;
using Quintilink.Views;

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
        private readonly IDialogService? _dialogService;
        private readonly IDispatcherService? _dispatcherService;
        private readonly ILogExportService _logExportService;

        // Collection to store log entries for export
        private readonly List<LogEntry> _logEntries = new();
        private readonly object _logEntriesLock = new();

        // Connection statistics
        private readonly ConnectionStatistics _statistics = new();
        private System.Threading.Timer? _statisticsUpdateTimer;

        // Hex viewer features
        private readonly List<LogBookmark> _bookmarks = new();
        private readonly List<ByteHighlightRange> _highlightRanges = new();
        private HexSearchFilter _currentSearchFilter = new();

        // Statistics window tracking
        private Views.StatisticsWindow? _statisticsWindow;
        private System.Threading.Timer? _statisticsWindowUpdateTimer;

        public IAsyncRelayCommand SendQuickMessageCommand { get; }

        [ObservableProperty]
        private string quickSendText = string.Empty;

        partial void OnQuickSendTextChanged(string value)
        {
            SendQuickMessageCommand.NotifyCanExecuteChanged();
        }

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

        public bool IsClientMode
        {
            get => !IsServerMode && !IsSerialMode;
            set
            {
                if (!value)
                    return;

                if (IsServerMode)
                    IsServerMode = false;

                if (IsSerialMode)
                    IsSerialMode = false;

                OnPropertyChanged(nameof(IsClientMode));
            }
        }

        partial void OnIsServerModeChanged(bool value)
        {
            if (value && IsSerialMode)
            {
                IsSerialMode = false;
            }

            OnPropertyChanged(nameof(IsClientMode));
        }

        partial void OnIsSerialModeChanged(bool value)
        {
            if (value && IsServerMode)
            {
                IsServerMode = false;
            }

            OnPropertyChanged(nameof(IsClientMode));
        }

        [ObservableProperty]
        private FlowDocument logDocument = LogHelper.CreateLogDocument();

        [ObservableProperty]
        private bool isConnected;

        partial void OnIsConnectedChanged(bool value)
        {
            SendQuickMessageCommand?.NotifyCanExecuteChanged();
        }

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

        // Modem line status properties
        [ObservableProperty]
        private bool ctsStatus;

        [ObservableProperty]
        private bool dsrStatus;

        [ObservableProperty]
        private bool cdStatus;

        [ObservableProperty]
        private bool riStatus;

        [ObservableProperty]
        private bool dtrEnabled;

        [ObservableProperty]
        private bool rtsEnabled;

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
        public string title = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;

        // Parameterless constructor for XAML designer support
        public MainViewModel() : this(null, null)
        {
        }

        // Constructor with dependency injection
        public MainViewModel(IDialogService? dialogService, IDispatcherService? dispatcherService)
        {
            _dialogService = dialogService;
            _dispatcherService = dispatcherService;
            _logExportService = new LogExportService();

            SendQuickMessageCommand = new AsyncRelayCommand(SendQuickMessage, CanSendQuickMessage);

            _client.DataReceived += OnDataReceived;

            _client.Disconnected += remote =>
        {
            InvokeOnUiThread(() =>
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
                // Track statistics
                _statistics.RecordReceived(data.Length);

                string hex = BitConverter.ToString(data).Replace("-", " ");
                string ascii = ConvertToReadableAscii(data);
                InvokeOnUiThread(() =>
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
            InvokeOnUiThread(() =>
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

            _serialPort.ModemLinesChanged += () =>
            {
                InvokeOnUiThread(() =>
         {
             UpdateModemLineStatus();
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
            string previousSelection = SelectedSerialPort;

            AvailableSerialPorts.Clear();

            // Sort COM ports numerically
            var ports = SerialPortWrapper.GetAvailablePorts()
                     .OrderBy(port =>
            {
                if (port.StartsWith("COM") && int.TryParse(port.Substring(3), out int portNumber))
                    return portNumber;
                return int.MaxValue;
            }).ToList();

            foreach (var port in ports)
            {
                AvailableSerialPorts.Add(port);
            }

            if (!string.IsNullOrEmpty(previousSelection) && AvailableSerialPorts.Contains(previousSelection))
            {
                SelectedSerialPort = previousSelection;
            }
        }

        [RelayCommand(CanExecute = nameof(IsSerialConnected))]
        private void ToggleDtr()
        {
            DtrEnabled = !DtrEnabled;
            _serialPort.SetDtrEnable(DtrEnabled);
            AppendLog($"[SYS] DTR {(DtrEnabled ? "enabled" : "disabled")}");
        }

        [RelayCommand(CanExecute = nameof(IsSerialConnected))]
        private void ToggleRts()
        {
            RtsEnabled = !RtsEnabled;
            _serialPort.SetRtsEnable(RtsEnabled);
            AppendLog($"[SYS] RTS {(RtsEnabled ? "enabled" : "disabled")}");
        }

        private bool IsSerialConnected() => IsConnected && IsSerialMode;

        private void UpdateModemLineStatus()
        {
            if (!IsSerialMode || !IsConnected)
            {
                CtsStatus = false;
                DsrStatus = false;
                CdStatus = false;
                RiStatus = false;
                return;
            }

            CtsStatus = _serialPort.CtsHolding;
            DsrStatus = _serialPort.DsrHolding;
            CdStatus = _serialPort.CDHolding;
            RiStatus = _serialPort.RingIndicator;
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
                // Start statistics tracking
                _statistics.Reset();
                _statistics.StartConnection();
                StartStatisticsTimer();

                if (IsSerialMode)
                {
                    await _serialPort.ConnectAsync(
                   SelectedSerialPort,
                SelectedBaudRate,
                       SelectedParity,
                        SelectedDataBits,
                       SelectedStopBits);
                    AppendLog($"[SYS] Serial port {SelectedSerialPort} opened at {SelectedBaudRate} baud");

                    IsConnected = true;

                    // Update modem line status
                    UpdateModemLineStatus();

                    // Get initial DTR/RTS states
                    DtrEnabled = _serialPort.GetDtrEnable();
                    RtsEnabled = _serialPort.GetRtsEnable();

                    // Notify command can execute changed AFTER IsConnected is set
                    ToggleDtrCommand.NotifyCanExecuteChanged();
                    ToggleRtsCommand.NotifyCanExecuteChanged();
                }
                else if (IsServerMode)
                {
                    await _server.StartAsync(Port);
                    AppendLog($"[SYS] Server started on port {Port}");
                    IsConnected = true;
                }
                else
                {
                    await _client.ConnectAsync(Host, Port);
                    AppendLog($"[SYS] Connected to {Host}:{Port}");
                    IsConnected = true;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[ERR] Failed to connect: {ex.Message}");
                IsConnected = false;
                _statistics.EndConnection();
                StopStatisticsTimer();
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

                UpdateModemLineStatus();

                ToggleDtrCommand.NotifyCanExecuteChanged();
                ToggleRtsCommand.NotifyCanExecuteChanged();
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
            
            // End statistics tracking
            _statistics.EndConnection();
            StopStatisticsTimer();
            
            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
            UpdateServerStatus();
        }

        private bool CanConnect() => !IsConnected && (!IsSerialMode || !string.IsNullOrEmpty(SelectedSerialPort));
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
                // Track statistics
                _statistics.RecordSent(bytes.Length);

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
        private async Task AddMessage()
        {
            var editor = new MessageEditorViewModel();

            if (_dialogService != null)
            {
                var result = await _dialogService.ShowDialogAsync(editor);
                if (result == true)
                {
                    PredefinedMessages.Add(editor.ToDefinition());
                    SaveMessages();
                }
            }
            else
            {
                var dlg = new Views.MessageEditorWindow
                {
                    DataContext = editor,
                    Owner = Application.Current?.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (dlg.ShowDialog() == true)
                {
                    PredefinedMessages.Add(editor.ToDefinition());
                    SaveMessages();
                }
            }
        }

        [RelayCommand]
        private async Task EditMessage(MessageDefinition? message)
        {
            if (message is null) return;
            var editor = new MessageEditorViewModel(message);

            if (_dialogService != null)
            {
                var result = await _dialogService.ShowDialogAsync(editor);
                if (result == true)
                {
                    var index = PredefinedMessages.IndexOf(message);
                    if (index >= 0)
                    {
                        PredefinedMessages[index] = editor.ToDefinition();
                        SaveMessages();
                    }
                }
            }
            else
            {
                var dlg = new Views.MessageEditorWindow
                {
                    DataContext = editor,
                    Owner = Application.Current?.MainWindow,
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
        private void ClearLog()
        {
            InvokeOnUiThread(() =>
            {
                LogDocument.Blocks.Clear();
                
                // Also clear stored log entries
                lock (_logEntriesLock)
                {
                    _logEntries.Clear();
                }
            });
        }

        [RelayCommand]
        private async Task ExportLog()
        {
            if (_logEntries.Count == 0)
            {
                _dialogService?.ShowMessage("Export Log", "No log entries to export.");
                return;
            }

            try
            {
                // Create SaveFileDialog
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export Log",
                    Filter = _logExportService.GetFileFilter(),
                    FilterIndex = 1,
                    FileName = $"Quintilink_Log_{DateTime.Now:yyyyMMdd_HHmmss}",
                    DefaultExt = ".csv"
                };

                bool? result = saveFileDialog.ShowDialog();

                if (result == true)
                {
                    // Determine format based on filter index
                    LogExportFormat format = saveFileDialog.FilterIndex switch
                    {
                        1 => LogExportFormat.Csv,
                        2 => LogExportFormat.Json,
                        3 => LogExportFormat.PlainText,
                        _ => LogExportFormat.Csv
                    };

                    List<LogEntry> entriesToExport;
                    lock (_logEntriesLock)
                    {
                        entriesToExport = new List<LogEntry>(_logEntries);
                    }

                    bool success = await _logExportService.ExportLogAsync(
                        saveFileDialog.FileName,
                        entriesToExport,
                        format
                    );

                    if (success)
                    {
                        AppendLog($"[SYS] Log exported to {saveFileDialog.FileName} ({entriesToExport.Count} entries)");
                        _dialogService?.ShowMessage("Export Successful", $"Log exported successfully to:\n{saveFileDialog.FileName}\n\nTotal entries: {entriesToExport.Count}");
                    }
                    else
                    {
                        AppendLog($"[ERR] Failed to export log to {saveFileDialog.FileName}");
                        _dialogService?.ShowMessage("Export Failed", "Failed to export log. Please check file permissions and try again.");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[ERR] Export error: {ex.Message}");
                _dialogService?.ShowMessage("Export Error", $"An error occurred during export:\n{ex.Message}");
            }
        }

        [RelayCommand]
        private async Task AddReaction()
        {
            var vm = new ResponseEditorViewModel();

            if (_dialogService != null)
            {
                var result = await _dialogService.ShowDialogAsync(vm);
                if (result == true)
                {
                    _reactions[vm.Trigger] = vm.ToDefinition();
                    RefreshReactions();
                    SaveMessages();
                }
            }
            else
            {
                var dlg = new Views.ResponseEditorWindow { DataContext = vm, Owner = Application.Current?.MainWindow };
                vm.RequestClose += result => dlg.DialogResult = result;

                if (dlg.ShowDialog() == true)
                {
                    _reactions[vm.Trigger] = vm.ToDefinition();
                    RefreshReactions();
                    SaveMessages();
                }
            }
        }

        [RelayCommand]
        private async Task EditReaction(KeyValuePair<string, MessageDefinition>? item)
        {
            if (item is null) return;

            var vm = new ResponseEditorViewModel(item.Value.Key, item.Value.Value);

            if (_dialogService != null)
            {
                var result = await _dialogService.ShowDialogAsync(vm);
                if (result == true)
                {
                    _reactions.Remove(item.Value.Key);
                    _reactions[vm.Trigger] = vm.ToDefinition();
                    RefreshReactions();
                    SaveMessages();
                }
            }
            else
            {
                var dlg = new Views.ResponseEditorWindow { DataContext = vm, Owner = Application.Current?.MainWindow };
                vm.RequestClose += result => dlg.DialogResult = result;

                if (dlg.ShowDialog() == true)
                {
                    _reactions.Remove(item.Value.Key);
                    _reactions[vm.Trigger] = vm.ToDefinition();
                    RefreshReactions();
                    SaveMessages();
                }
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
            // Track statistics
            _statistics.RecordReceived(data.Length);

            string ascii = ConvertToReadableAscii(data);
            string hex = BitConverter.ToString(data).Replace("-", " ");

            AppendLog($"[RX] ASCII: {ascii}");
            AppendLog($"[RX] HEX  : {hex}");

            await CheckReaction(hex);
        }

        private static string ConvertToReadableAscii(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 2);
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

            await SendMessageAsync(response);
        }

        private void AppendLog(string entry)
        {
            var now = DateTime.Now;
            string timestamp = now.ToString("HH:mm:ss");
            InvokeOnUiThread(() =>
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

                LogHelper.AppendLogEntry(LogDocument, timestamp, prefix, content, isAsciiLine);

                // Store log entry for export
                StoreLogEntry(now, entry, prefix.Trim());
            });
        }

        private void StoreLogEntry(DateTime timestamp, string entry, string prefix)
        {
            // Parse the entry to create a LogEntry object
            string direction = "SYS"; // Default
            string hexData = "";
            string asciiData = "";
            string message = entry;
            int byteCount = 0;

            // Extract direction from prefix
            if (prefix.Contains("[RX]")) direction = "RX";
            else if (prefix.Contains("[TX]")) direction = "TX";
            else if (prefix.Contains("[ERR]")) direction = "ERR";
            else if (prefix.Contains("[SYS]")) direction = "SYS";

            // Parse hex and ASCII data
            if (entry.Contains("HEX") && entry.Contains(":"))
            {
                var hexStart = entry.IndexOf("HEX");
                var colonIndex = entry.IndexOf(":", hexStart);
                if (colonIndex > 0)
                {
                    var dashIndex = entry.IndexOf("–", colonIndex);
                    if (dashIndex > 0)
                    {
                        hexData = entry.Substring(colonIndex + 1, dashIndex - colonIndex - 1).Trim();
                        var bytesText = entry.Substring(dashIndex + 1).Trim();
                        if (bytesText.Contains(" bytes"))
                        {
                            var parts = bytesText.Split(' ');
                            if (parts.Length > 0 && int.TryParse(parts[0], out int count))
                            {
                                byteCount = count;
                            }
                        }
                    }
                    else
                    {
                        hexData = entry.Substring(colonIndex + 1).Trim();
                    }
                }
            }
            else if (entry.Contains("ASCII:"))
            {
                var asciiStart = entry.IndexOf("ASCII:");
                asciiData = entry.Substring(asciiStart + 6).Trim();
            }

            var logEntry = new LogEntry(
                timestamp,
                direction,
                hexData,
                asciiData,
                message,
                byteCount,
                isBookmarked: false
            );

            lock (_logEntriesLock)
            {
                _logEntries.Add(logEntry);
            }
        }

        [RelayCommand]
        private void AddBookmark()
        {
            int currentIndex;
            lock (_logEntriesLock)
            {
                currentIndex = _logEntries.Count - 1;
            }

            if (currentIndex < 0)
            {
                _dialogService?.ShowMessage("Add Bookmark", "No log entries to bookmark.");
                return;
            }

            // Toggle breakpoint-like bookmark for the specific log entry
            LogEntry entry;
            lock (_logEntriesLock)
            {
                entry = _logEntries[currentIndex];
                entry.IsBookmarked = !entry.IsBookmarked;
            }

            if (entry.IsBookmarked)
            {
                var bookmark = new LogBookmark
                {
                    LogEntryIndex = currentIndex,
                    Description = $"Bookmark at {entry.Timestamp:HH:mm:ss}",
                    LogEntryPreview = entry.Message
                };

                _bookmarks.RemoveAll(b => b.LogEntryIndex == currentIndex);
                _bookmarks.Add(bookmark);
            }
            else
            {
                _bookmarks.RemoveAll(b => b.LogEntryIndex == currentIndex);
            }

            // Update the visual line in the FlowDocument to show/remove breakpoint dot
            InvokeOnUiThread(() => ToggleBookmarkDotInDocument(currentIndex, entry.IsBookmarked));
        }

        private void ToggleBookmarkDotInDocument(int logEntryIndex, bool isBookmarked)
        {
            if (LogDocument.Blocks.Count == 0)
                return;

            var paragraph = LogDocument.Blocks.ElementAtOrDefault(logEntryIndex) as Paragraph;
            if (paragraph == null)
                return;

            // Recreate line based on stored LogEntry so formatting stays correct
            LogEntry entry;
            lock (_logEntriesLock)
            {
                if (logEntryIndex < 0 || logEntryIndex >= _logEntries.Count)
                    return;

                entry = _logEntries[logEntryIndex];
            }

            bool isAsciiLine = entry.Message.StartsWith("[RX] ASCII:") || entry.Message.StartsWith("[TX] ASCII:");

            string prefix = "";
            string content = entry.Message;

            if (content.StartsWith("["))
            {
                int endBracket = content.IndexOf(']');
                if (endBracket > 0)
                {
                    prefix = content.Substring(0, endBracket + 1) + " ";
                    content = content.Substring(endBracket + 2);
                }
            }

            // Render into a temp doc then copy inlines over
            var tempDoc = LogHelper.CreateLogDocument();
            LogHelper.AppendLogEntry(tempDoc, entry.Timestamp.ToString("HH:mm:ss"), prefix, content, isAsciiLine, isBookmarked);

            var tmpParagraph = tempDoc.Blocks.LastBlock as Paragraph;
            if (tmpParagraph == null)
                return;

            paragraph.Inlines.Clear();
            foreach (var inline in tmpParagraph.Inlines.ToList())
            {
                tmpParagraph.Inlines.Remove(inline);
                paragraph.Inlines.Add(inline);
            }
        }

        private void InvokeOnUiThread(Action action)
        {
            if (_dispatcherService != null)
            {
                _dispatcherService.Invoke(action);
            }
            else
            {
                Application.Current?.Dispatcher?.Invoke(action);
            }
        }

        private void StartStatisticsTimer()
        {
            _statisticsUpdateTimer = new System.Threading.Timer(_ =>
            {
                // Timer callback runs on thread pool, no UI updates needed here
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private void StopStatisticsTimer()
        {
            _statisticsUpdateTimer?.Dispose();
            _statisticsUpdateTimer = null;
        }

        private async Task SendQuickMessage()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(QuickSendText))
                {
                    AppendLog("[ERR] Quick Send: Text is empty");
                    return;
                }

                AppendLog($"[SYS] Quick Send: Preparing to send '{QuickSendText}'");

                var bytes = MixedInputParser.Parse(QuickSendText, out int invalidHexSegmentCount);
                if (invalidHexSegmentCount > 0)
                {
                    AppendLog($"[SYS] Quick Send: {invalidHexSegmentCount} invalid <...> segment(s) treated as ASCII");
                }

                bool success;
                if (IsSerialMode)
                    success = await _serialPort.SendAsync(bytes);
                else if (IsServerMode)
                    success = await _server.SendAsync(bytes);
                else
                    success = await _client.SendAsync(bytes);

                if (success)
                {
                    _statistics.RecordSent(bytes.Length);

                    string ascii = ConvertToReadableAscii(bytes);
                    string hex = MessageDefinition.ToSpacedHex(bytes);
                    AppendLog($"[TX] ASCII: {ascii}");
                    AppendLog($"[TX] HEX  : {hex} – {bytes.Length} bytes");

                    QuickSendText = string.Empty;
                }
                else
                {
                    AppendLog("[ERR] Quick Send failed: not connected");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[ERR] Quick Send failed: {ex.Message}");
                AppendLog($"[ERR] Stack trace: {ex.StackTrace}");
            }
        }

        private bool CanSendQuickMessage() => IsConnected && !string.IsNullOrWhiteSpace(QuickSendText);

        [RelayCommand]
        private void ShowStatistics()
        {
            InvokeOnUiThread(() =>
            {
                if (_statisticsWindow == null)
                {
                    var vm = new StatisticsViewModel(_statistics);

                    _statisticsWindow = new StatisticsWindow
                    {
                        DataContext = vm,
                        Owner = Application.Current?.MainWindow,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    _statisticsWindow.Closed += (_, __) =>
                    {
                        _statisticsWindowUpdateTimer?.Dispose();
                        _statisticsWindowUpdateTimer = null;
                        _statisticsWindow = null;
                    };

                    _statisticsWindow.Show();

                    // update stats ~4x/sec while window is open
                    _statisticsWindowUpdateTimer?.Dispose();
                    _statisticsWindowUpdateTimer = new System.Threading.Timer(_ =>
                    {
                        InvokeOnUiThread(() =>
                        {
                            if (_statisticsWindow?.DataContext is StatisticsViewModel svm)
                                svm.UpdateStatistics();
                        });
                    }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(250));
                }
                else
                {
                    if (!_statisticsWindow.IsVisible)
                        _statisticsWindow.Show();

                    _statisticsWindow.Activate();
                }
            });
        }

        [RelayCommand]
        private async Task SearchHexPattern()
        {
            var vm = new SearchDialogViewModel();

            bool? result;
            if (_dialogService != null)
            {
                result = await _dialogService.ShowDialogAsync(vm);
            }
            else
            {
                var dlg = new SearchDialog
                {
                    DataContext = vm,
                    Owner = Application.Current?.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                vm.RequestClose += r => dlg.DialogResult = r;
                result = dlg.ShowDialog();
            }

            if (result != true)
                return;

            // Store last filter
            _currentSearchFilter = new HexSearchFilter
            {
                Pattern = vm.SearchPattern,
                UseRegex = vm.UseRegex,
                CaseSensitive = vm.CaseSensitive,
                Direction = vm.SelectedDirection
            };

            List<LogEntry> snapshot;
            lock (_logEntriesLock)
            {
                snapshot = new List<LogEntry>(_logEntries);
            }

            var matches = SearchInLogEntries(snapshot, _currentSearchFilter);

            InvokeOnUiThread(() =>
            {
                var resultsVm = new SearchResultsViewModel();
                resultsVm.LoadResults(vm.SearchPattern, matches);

                var wnd = new SearchResultsWindow
                {
                    DataContext = resultsVm,
                    Owner = Application.Current?.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                wnd.Show();
                wnd.Activate();
            });
        }

        private static List<LogEntry> SearchInLogEntries(List<LogEntry> entries, HexSearchFilter filter)
        {
            if (string.IsNullOrWhiteSpace(filter.Pattern))
                return new List<LogEntry>();

            var comparison = filter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            bool DirectionMatch(LogEntry e)
            {
                if (filter.Direction == SearchDirection.All) return true;
                return e.Direction.Equals(filter.Direction.ToString(), comparison);
            }

            if (filter.UseRegex)
            {
                var options = filter.CaseSensitive ? System.Text.RegularExpressions.RegexOptions.None : System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                var rx = new System.Text.RegularExpressions.Regex(filter.Pattern, options);
                return entries.Where(e => DirectionMatch(e) && rx.IsMatch(e.Message ?? string.Empty)).ToList();
            }

            return entries
                .Where(e => DirectionMatch(e) && (e.Message ?? string.Empty).Contains(filter.Pattern, comparison))
                .ToList();
        }
    }
}
