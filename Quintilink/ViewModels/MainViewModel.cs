using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Text.RegularExpressions;
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
        private readonly List<int> _visibleLogEntryIndices = new();

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

        [ObservableProperty]
        private bool showRxLogs = true;

        [ObservableProperty]
        private bool showTxLogs = true;

        [ObservableProperty]
        private bool showSysLogs = true;

        [ObservableProperty]
        private bool showErrLogs = true;

        [ObservableProperty]
        private bool showBookmarkedOnly;

        [ObservableProperty]
        private bool useRegexLogFilter;

        [ObservableProperty]
        private string logFilterText = string.Empty;

        public ObservableCollection<string> QuickSendHistory { get; } = new();
        public ObservableCollection<string> QuickSendPinnedSnippets { get; } = new();

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

        // Reactions now stored as list to allow multiple responses per trigger
        private readonly List<ReactionItem> _reactions = new();

        public ObservableCollection<ReactionItem> Reactions { get; } = new();

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
            foreach (var dto in loaded.ReactionsList)
            {
                var (trigger, def) = dto.ToReaction();
                _reactions.Add(new ReactionItem(trigger, def));
            }

            RefreshReactions();

            // load settings
            _settings = AppSettings.Load();
            Host = _settings.Host;
            Port = _settings.Port;

            QuickSendHistory.Clear();
            foreach (var entry in _settings.QuickSendHistory)
                QuickSendHistory.Add(entry);

            QuickSendPinnedSnippets.Clear();
            foreach (var snippet in _settings.QuickSendPinnedSnippets)
                QuickSendPinnedSnippets.Add(snippet);

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

        partial void OnShowRxLogsChanged(bool value) => RebuildVisibleLogDocument();
        partial void OnShowTxLogsChanged(bool value) => RebuildVisibleLogDocument();
        partial void OnShowSysLogsChanged(bool value) => RebuildVisibleLogDocument();
        partial void OnShowErrLogsChanged(bool value) => RebuildVisibleLogDocument();
        partial void OnShowBookmarkedOnlyChanged(bool value) => RebuildVisibleLogDocument();
        partial void OnUseRegexLogFilterChanged(bool value) => RebuildVisibleLogDocument();
        partial void OnLogFilterTextChanged(string value) => RebuildVisibleLogDocument();

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
                ReactionsList = _reactions
                    .Select(r => new ReactionDto(r.Trigger, r.Response))
                    .ToList()
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
                    _visibleLogEntryIndices.Clear();
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
                    _reactions.Add(new ReactionItem(vm.Trigger, vm.ToDefinition()));
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
                    _reactions.Add(new ReactionItem(vm.Trigger, vm.ToDefinition()));
                    RefreshReactions();
                    SaveMessages();
                }
            }
        }

        [RelayCommand]
        private async Task EditReaction(ReactionItem? item)
        {
            if (item is null) return;

            var vm = new ResponseEditorViewModel(item.Trigger, item.Response);

            if (_dialogService != null)
            {
                var result = await _dialogService.ShowDialogAsync(vm);
                if (result == true)
                {
                    var idx = _reactions.IndexOf(item);
                    if (idx >= 0)
                    {
                        _reactions[idx] = new ReactionItem(vm.Trigger, vm.ToDefinition());
                        RefreshReactions();
                        SaveMessages();
                    }
                }
            }
            else
            {
                var dlg = new Views.ResponseEditorWindow { DataContext = vm, Owner = Application.Current?.MainWindow };
                vm.RequestClose += result => dlg.DialogResult = result;

                if (dlg.ShowDialog() == true)
                {
                    var idx = _reactions.IndexOf(item);
                    if (idx >= 0)
                    {
                        _reactions[idx] = new ReactionItem(vm.Trigger, vm.ToDefinition());
                        RefreshReactions();
                        SaveMessages();
                    }
                }
            }
        }

        [RelayCommand]
        private void DeleteReaction(ReactionItem? item)
        {
            if (item is null) return;

            if (_reactions.Remove(item))
            {
                RefreshReactions();
                SaveMessages();
            }
        }

        [RelayCommand]
        private void TogglePauseReaction(ReactionItem? item)
        {
            if (item is null) return;

            item.Response.IsPaused = !item.Response.IsPaused;

            RefreshReactions();
            SaveMessages();
        }

        private void RefreshReactions()
        {
            Reactions.Clear();
            foreach (var r in _reactions)
                Reactions.Add(r);
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
            var exactMatches = _reactions
                .Where(item => string.Equals(item.Trigger, hex, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.Response.Priority)
                .ToList();

            if (exactMatches.Count > 0)
            {
                await ExecuteReactionMatches(exactMatches);
                return;
            }

            var prefixMatches = _reactions
                .Where(item => hex.StartsWith(item.Trigger, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.Response.Priority)
                .ToList();

            await ExecuteReactionMatches(prefixMatches);
        }

        private async Task ExecuteReactionMatches(List<ReactionItem> matches)
        {
            foreach (var item in matches)
            {
                if (item.Response.IsPaused)
                    continue;

                await SendReaction(item.Trigger, item.Response);

                if (item.Response.StopAfterMatch)
                    break;
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

            RebuildVisibleLogDocument();
        }

        [RelayCommand]
        private void ToggleBookmarkAtIndex(int logEntryIndex)
        {
            if (logEntryIndex < 0)
                return;

            LogEntry entry;
            lock (_logEntriesLock)
            {
                if (logEntryIndex >= _logEntries.Count)
                    return;

                entry = _logEntries[logEntryIndex];
                entry.IsBookmarked = !entry.IsBookmarked;
            }

            if (entry.IsBookmarked)
            {
                var bookmark = new LogBookmark
                {
                    LogEntryIndex = logEntryIndex,
                    Description = $"Bookmark at {entry.Timestamp:HH:mm:ss}",
                    LogEntryPreview = entry.Message
                };

                _bookmarks.RemoveAll(b => b.LogEntryIndex == logEntryIndex);
                _bookmarks.Add(bookmark);
            }
            else
            {
                _bookmarks.RemoveAll(b => b.LogEntryIndex == logEntryIndex);
            }

            RebuildVisibleLogDocument();
        }

        [RelayCommand]
        private void ToggleBookmarkAtVisibleIndex(int visibleIndex)
        {
            int logEntryIndex;
            lock (_logEntriesLock)
            {
                if (visibleIndex < 0 || visibleIndex >= _visibleLogEntryIndices.Count)
                    return;

                logEntryIndex = _visibleLogEntryIndices[visibleIndex];
            }

            ToggleBookmarkAtIndex(logEntryIndex);
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

        [RelayCommand]
        private void ClearLogFilters()
        {
            ShowRxLogs = true;
            ShowTxLogs = true;
            ShowSysLogs = true;
            ShowErrLogs = true;
            ShowBookmarkedOnly = false;
            UseRegexLogFilter = false;
            LogFilterText = string.Empty;
            RebuildVisibleLogDocument();
        }

        private void RebuildVisibleLogDocument()
        {
            InvokeOnUiThread(() =>
            {
                LogDocument.Blocks.Clear();

                List<LogEntry> snapshot;
                lock (_logEntriesLock)
                {
                    snapshot = new List<LogEntry>(_logEntries);
                    _visibleLogEntryIndices.Clear();
                }

                for (var i = 0; i < snapshot.Count; i++)
                {
                    var entry = snapshot[i];
                    if (!PassesLogFilters(entry))
                        continue;

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

                    LogHelper.AppendLogEntry(LogDocument, entry.Timestamp.ToString("HH:mm:ss"), prefix, content, isAsciiLine, entry.IsBookmarked);

                    lock (_logEntriesLock)
                    {
                        _visibleLogEntryIndices.Add(i);
                    }
                }
            });
        }

        private bool PassesLogFilters(LogEntry entry)
        {
            bool directionVisible = entry.Direction switch
            {
                "RX" => ShowRxLogs,
                "TX" => ShowTxLogs,
                "SYS" => ShowSysLogs,
                "ERR" => ShowErrLogs,
                _ => true
            };

            if (!directionVisible)
                return false;

            if (ShowBookmarkedOnly && !entry.IsBookmarked)
                return false;

            if (string.IsNullOrWhiteSpace(LogFilterText))
                return true;

            if (UseRegexLogFilter)
            {
                try
                {
                    return Regex.IsMatch(entry.Message ?? string.Empty, LogFilterText, RegexOptions.IgnoreCase);
                }
                catch
                {
                    return true;
                }
            }

            return (entry.Message ?? string.Empty).Contains(LogFilterText, StringComparison.OrdinalIgnoreCase);
        }

        private void AddQuickSendHistoryEntry(string text)
        {
            var value = text?.Trim();
            if (string.IsNullOrWhiteSpace(value))
                return;

            var existingIndex = QuickSendHistory.IndexOf(value);
            if (existingIndex >= 0)
                QuickSendHistory.RemoveAt(existingIndex);

            QuickSendHistory.Insert(0, value);
            while (QuickSendHistory.Count > 25)
                QuickSendHistory.RemoveAt(QuickSendHistory.Count - 1);

            SaveQuickSendCollections();
        }

        private void SaveQuickSendCollections()
        {
            _settings.QuickSendHistory = QuickSendHistory.ToList();
            _settings.QuickSendPinnedSnippets = QuickSendPinnedSnippets.ToList();
            _settings.Save();
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

                    AddQuickSendHistoryEntry(QuickSendText);

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
        private void PinQuickSendSnippet()
        {
            var snippet = QuickSendText?.Trim();
            if (string.IsNullOrWhiteSpace(snippet))
                return;

            if (!QuickSendPinnedSnippets.Contains(snippet))
            {
                QuickSendPinnedSnippets.Insert(0, snippet);
                while (QuickSendPinnedSnippets.Count > 20)
                    QuickSendPinnedSnippets.RemoveAt(QuickSendPinnedSnippets.Count - 1);
                SaveQuickSendCollections();
            }
        }

        [RelayCommand]
        private void ApplyPinnedSnippet(string? snippet)
        {
            if (!string.IsNullOrWhiteSpace(snippet))
                QuickSendText = snippet;
        }

        [RelayCommand]
        private void RemovePinnedSnippet(string? snippet)
        {
            if (string.IsNullOrWhiteSpace(snippet))
                return;

            if (QuickSendPinnedSnippets.Remove(snippet))
                SaveQuickSendCollections();
        }

        [RelayCommand]
        private void ClearQuickSendHistory()
        {
            QuickSendHistory.Clear();
            SaveQuickSendCollections();
        }

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
        private void ShowAbout()
        {
            InvokeOnUiThread(() =>
            {
                var vm = new AboutViewModel();
                var aboutWindow = new Views.AboutWindow
                {
                    DataContext = vm,
                    Owner = Application.Current?.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                aboutWindow.ShowDialog();
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

        [RelayCommand]
        private void CompareMessages()
        {
            InvokeOnUiThread(() =>
            {
                var vm = new HexComparisonViewModel();
                vm.LoadMessages(PredefinedMessages);

                var wnd = new Views.HexComparisonWindow
                {
                    DataContext = vm,
                    Owner = Application.Current?.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                wnd.Show();
            });
        }
    }
}
