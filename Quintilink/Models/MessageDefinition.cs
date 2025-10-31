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
                var sb = new StringBuilder();
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

    public static string ToSpacedHex(byte[] bytes) =>
        string.Join(" ", bytes.Select(b => b.ToString("X2")));

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

        return Enumerable.Range(0, clean.Length / 2)
                         .Select(i => Convert.ToByte(clean.Substring(i * 2, 2), 16))
                         .ToArray();
    }
}
