using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text;
using System.Text.RegularExpressions;
using Quintilink.Models;

namespace Quintilink.ViewModels
{
    public partial class MessageEditorViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsValid))]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string name = string.Empty;

        [ObservableProperty]
        private string ascii = string.Empty;

        [ObservableProperty]
        private string hex = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsValid))]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private bool isHexValid = true;

        private bool _isUpdating;
        public bool IsValid => !string.IsNullOrWhiteSpace(Name) && IsHexValid;

        // Raised when the editor wants to close
        public event Action<bool>? RequestClose;

        public MessageEditorViewModel() { }

        public MessageEditorViewModel(MessageDefinition def)
        {
            Name = def.Name;
            Hex = MessageDefinition.ToSpacedHex(def.GetBytes());
            // Use macro-based ASCII representation
            Ascii = CollapseToAscii(def.GetBytes());
        }

        partial void OnAsciiChanged(string value)
        {
            if (_isUpdating) return;
            try
            {
                _isUpdating = true;
                var bytes = ExpandAsciiToBytes(value ?? string.Empty);
                Hex = ToHex(bytes); // user-friendly grouping with spaces
            }
            finally { _isUpdating = false; }
        }

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
                }
                else
                {
                    Ascii = string.Empty;
                }
            }
            finally { _isUpdating = false; }
        }

        public void NormalizeHexField()
        {
            if (string.IsNullOrWhiteSpace(Hex)) return;
            Hex = NormalizeHex(Hex);
            UpdateAsciiFromHex(Hex);
        }

        public MessageDefinition ToDefinition()
        {
            return new MessageDefinition(Name, Hex); // ctor compacts automatically
        }

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
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append(MacroDefinitions.CollapseByte(b));
            }
            return sb.ToString();
        }

        private void UpdateAsciiFromHex(string hexInput)
        {
            IsHexValid = ValidateHex(hexInput);
            if (!IsHexValid)
            {
                Ascii = string.Empty;
                return;
            }

            var bytes = FromHex(hexInput);
            Ascii = CollapseToAscii(bytes);

            Hex = MessageDefinition.ToSpacedHex(bytes);
        }

        private static string ToHex(byte[] bytes) =>
            string.Join(" ", bytes.Select(b => b.ToString("X2")));

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
            if (string.IsNullOrWhiteSpace(hex)) return true;
            return Regex.IsMatch(hex.Replace(" ", ""), @"^[0-9A-Fa-f]*$");
        }

        private static byte[] FromHex(string hex)
        {
            var clean = Regex.Replace(hex, @"[^0-9A-Fa-f]", "");
            if (clean.Length % 2 != 0) return Array.Empty<byte>();
            return Enumerable.Range(0, clean.Length / 2)
                             .Select(i => Convert.ToByte(clean.Substring(i * 2, 2), 16))
                             .ToArray();
        }

        [RelayCommand(CanExecute = nameof(IsValid))]
        private void Save()
        {
            RequestClose?.Invoke(true); // signal dialog close with OK
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(false); // signal dialog close with Cancel
        }
    }
}
