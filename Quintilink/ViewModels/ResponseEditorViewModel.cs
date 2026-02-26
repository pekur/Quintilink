using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Linq;
using Quintilink.Models;

namespace Quintilink.ViewModels;

public partial class ResponseEditorViewModel : ObservableObject
{
    private bool _isUpdating;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string trigger = string.Empty;

    [ObservableProperty]
    private string triggerAscii = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string name = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string hex = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string delayMs = "0";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string priority = "0";

    [ObservableProperty]
    private bool stopAfterMatch;

    [ObservableProperty]
    private string ascii = string.Empty;

    [ObservableProperty]
    private bool isHexValid = true;

    public bool IsDelayValid => int.TryParse(DelayMs, out var ms) && ms >= 0;
    public bool IsPriorityValid => int.TryParse(Priority, out _);
    public bool IsValid =>
    !string.IsNullOrWhiteSpace(Trigger) &&
  !string.IsNullOrWhiteSpace(Name) &&
        ValidateHex(Trigger) &&
      ValidateHex(Hex) &&
           IsDelayValid &&
           IsPriorityValid;

    public ResponseEditorViewModel() { }

    public ResponseEditorViewModel(string trigger, MessageDefinition def)
    {
        Trigger = trigger;
        TriggerAscii = CollapseToAscii(FromHex(trigger));
        Name = def.Name;
        Hex = MessageDefinition.ToSpacedHex(def.GetBytes());
        // Use macro-based ASCII representation
        Ascii = CollapseToAscii(def.GetBytes());
        DelayMs = def.DelayMs.ToString();
        Priority = def.Priority.ToString();
        StopAfterMatch = def.StopAfterMatch;
    }

    // --- Commands ---
    [RelayCommand(CanExecute = nameof(IsValid))]
    private void Save()
    {
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }

    public event Action<bool>? RequestClose;

    // --- Conversion helpers ---
    partial void OnHexChanged(string value)
    {
        if (_isUpdating) return;
        try
        {
            _isUpdating = true;
            IsHexValid = ValidateHex(value);

            if (IsHexValid)
            {
                var bytes = FromHex(value ?? string.Empty);
                Ascii = CollapseToAscii(bytes);
                Hex = MessageDefinition.ToSpacedHex(bytes);
            }
            else
            {
                Ascii = string.Empty;
            }
        }
        finally { _isUpdating = false; }
    }

    partial void OnAsciiChanged(string value)
    {
        if (_isUpdating) return;
        try
        {
            _isUpdating = true;
            var bytes = ExpandAsciiToBytes(value ?? string.Empty);
            Hex = MessageDefinition.ToSpacedHex(bytes);
            IsHexValid = true;
        }
        finally { _isUpdating = false; }
    }

    partial void OnTriggerChanged(string value)
    {
        if (_isUpdating) return;
        try
        {
            _isUpdating = true;
            if (string.IsNullOrWhiteSpace(value))
            {
                TriggerAscii = string.Empty;
                return;
            }

            // Normalize just like Hex
            var clean = Regex.Replace(value, @"[^0-9A-Fa-f]", "");
            if (clean.Length % 2 == 0)
            {
                Trigger = NormalizeHex(value);
                TriggerAscii = CollapseToAscii(FromHex(Trigger));
            }
        }
        finally { _isUpdating = false; }
    }

    partial void OnTriggerAsciiChanged(string value)
    {
        if (_isUpdating) return;
        try
        {
            _isUpdating = true;
            var bytes = ExpandAsciiToBytes(value ?? string.Empty);
            Trigger = MessageDefinition.ToSpacedHex(bytes);
        }
        finally { _isUpdating = false; }
    }

    public void NormalizeHexField()
    {
        if (string.IsNullOrWhiteSpace(Hex)) return;
        Hex = NormalizeHex(Hex);
        OnHexChanged(Hex);
    }

    public MessageDefinition ToDefinition()
    {
        var delay = int.TryParse(DelayMs, out var ms) ? ms : 0;
        var priorityValue = int.TryParse(Priority, out var p) ? p : 0;
        return new MessageDefinition(Name, Hex, delay)
        {
            Priority = priorityValue,
            StopAfterMatch = StopAfterMatch
        };
    }

    // --- static utils ---
    private static byte[] ExpandAsciiToBytes(string ascii)
    {
        if (string.IsNullOrEmpty(ascii)) return Array.Empty<byte>();
        var output = new List<byte>();

        for (int i = 0; i < ascii.Length; i++)
        {
            if (ascii[i] == '<')
            {
                int end = ascii.IndexOf('>', i);
                if (end > i)
                {
                    string macro = ascii.Substring(i, end - i + 1);
                    var expanded = MacroDefinitions.ExpandMacro(macro);
                    if (expanded.Length > 0)
                    {
                        output.AddRange(expanded);
                        i = end;
                        continue;
                    }
                }
            }
            output.Add((byte)ascii[i]);
        }

        return output.ToArray();
    }

    private static string CollapseToAscii(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(MacroDefinitions.CollapseByte(b));
        return sb.ToString();
    }

    private static byte[] FromHex(string hex)
    {
        var clean = Regex.Replace(hex ?? string.Empty, @"[^0-9A-Fa-f]", "");
        if (clean.Length % 2 != 0) return Array.Empty<byte>();
        
        int byteCount = clean.Length / 2;
        byte[] result = new byte[byteCount];

        for (int i = 0; i < byteCount; i++)
        {
            int pos = i * 2;
            int high = HexCharToValue(clean[pos]);
            int low = HexCharToValue(clean[pos + 1]);
            result[i] = (byte)((high << 4) | low);
        }

        return result;
    }

    private static int HexCharToValue(char c)
    {
        if (c >= '0' && c <= '9') return c - '0';
        if (c >= 'A' && c <= 'F') return c - 'A' + 10;
        if (c >= 'a' && c <= 'f') return c - 'a' + 10;
        return 0;
    }

    private static string NormalizeHex(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var clean = Regex.Replace(input, @"[^0-9A-Fa-f]", "").ToUpperInvariant();
        var pairs = new List<string>();
        for (int i = 0; i < clean.Length; i += 2)
        {
            if (i + 2 <= clean.Length)
                pairs.Add(clean.Substring(i, 2));
        }
        return string.Join(" ", pairs);
    }

    private static bool ValidateHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return false;
        return Regex.IsMatch(hex, @"^([0-9A-Fa-f]{2}\s?)*$");
    }
}
