using System.Text;
using System.Text.RegularExpressions;
using Quintilink.Models;

public class MessageDefinition
{
    public string Name { get; set; } = string.Empty;

    // Always compact HEX internally
    public string Content { get; set; } = string.Empty;

    public int DelayMs { get; set; }


    public MessageDefinition() { }

    public MessageDefinition(string name, string hexContent, int delayMs = 0)
    {
        Name = name;
        Content = CompactHex(hexContent);
        DelayMs = delayMs;
    }

    public byte[] GetBytes()
    {
        return ParseHex(Content);
    }

    public string GetAscii()
    {
        return Encoding.ASCII.GetString(GetBytes());
    }

    // Property for displaying formatted ASCII with macros
    public string DisplayAscii
    {
        get
        {
            try
            {
                var bytes = GetBytes();
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes)
                {
                    sb.Append(MacroDefinitions.CollapseByte(b));
                }
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    // Property for displaying spaced HEX
    public string DisplayHex
    {
        get
        {
            try
            {
                return ToSpacedHex(GetBytes());
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public static string ToSpacedHex(byte[] bytes)
    {
        if (bytes.Length == 0) return string.Empty;

        // Pre-calculate exact size: "XX " per byte, minus last space
        int length = bytes.Length * 3 - 1;

        return string.Create(length, bytes, (span, data) =>
        {
            const string hexChars = "0123456789ABCDEF";
            int pos = 0;

            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];
                span[pos++] = hexChars[b >> 4];      // High nibble
                span[pos++] = hexChars[b & 0x0F];    // Low nibble

                if (i < data.Length - 1)
                    span[pos++] = ' ';
            }
        });
    }

    public static string CompactHex(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return Regex.Replace(input, @"[^0-9A-Fa-f]", "").ToUpperInvariant();
    }

    private static byte[] ParseHex(string input)
    {
        string clean = CompactHex(input);
        if (clean.Length % 2 != 0)
            throw new FormatException("Invalid hex string length.");

        int byteCount = clean.Length / 2;
        byte[] result = new byte[byteCount];

        for (int i = 0; i < byteCount; i++)
        {
            int pos = i * 2;
            // Parse directly from string without Substring allocation
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
        throw new FormatException($"Invalid hex character: {c}");
    }
}
