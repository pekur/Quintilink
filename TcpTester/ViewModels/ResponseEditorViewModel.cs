using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Linq;
using TcpTester.Models;

namespace TcpTester.ViewModels;

public partial class ResponseEditorViewModel : ObservableObject
{
    private bool _isUpdating;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string trigger = string.Empty;

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
    private string ascii = string.Empty;

    [ObservableProperty]
    private bool isHexValid = true;

    public bool IsDelayValid => int.TryParse(DelayMs, out var ms) && ms >= 0;
    public bool IsValid =>
                !string.IsNullOrWhiteSpace(Trigger) &&
                !string.IsNullOrWhiteSpace(Name) &&
                ValidateHex(Trigger) &&
                ValidateHex(Hex) &&
                IsDelayValid;

    public ResponseEditorViewModel() { }

    public ResponseEditorViewModel(string trigger, MessageDefinition def)
    {
        Trigger = trigger;
        Name = def.Name;
        Hex = MessageDefinition.ToSpacedHex(def.GetBytes());
        Ascii = def.GetAscii();
        DelayMs = def.DelayMs.ToString();
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

    partial void OnTriggerChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        // Normalize just like Hex
        var clean = Regex.Replace(value, @"[^0-9A-Fa-f]", "");
        if (clean.Length % 2 == 0)
            Trigger = NormalizeHex(value);  // reuse the same NormalizeHex you already have
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
        return new MessageDefinition(Name, Hex, delay);
    }

    // --- static utils copied from MessageEditorViewModel ---
    private static string CollapseToAscii(byte[] bytes)
    {
        var sb = new StringBuilder();
        foreach (var b in bytes)
            sb.Append(MacroDefinitions.CollapseByte(b));
        return sb.ToString();
    }

    private static byte[] FromHex(string hex)
    {
        var clean = Regex.Replace(hex ?? string.Empty, @"[^0-9A-Fa-f]", "");
        if (clean.Length % 2 != 0) return Array.Empty<byte>();
        return Enumerable.Range(0, clean.Length / 2)
                         .Select(i => Convert.ToByte(clean.Substring(i * 2, 2), 16))
                         .ToArray();
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
