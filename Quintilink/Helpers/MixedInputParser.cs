using System.Text;
using System.Text.RegularExpressions;

namespace Quintilink.Helpers
{
    internal static class MixedInputParser
    {
        public static byte[] Parse(string input) => Parse(input, out _);

        public static byte[] Parse(string input, out int invalidHexSegmentCount)
        {
            invalidHexSegmentCount = 0;

            if (string.IsNullOrEmpty(input))
                return [];

            var buffer = new List<byte>(input.Length);

            int i = 0;
            while (i < input.Length)
            {
                int open = input.IndexOf('<', i);
                if (open < 0)
                {
                    AppendAsciiWithMacros(buffer, input.AsSpan(i));
                    break;
                }

                if (open > i)
                    AppendAsciiWithMacros(buffer, input.AsSpan(i, open - i));

                int close = input.IndexOf('>', open + 1);
                if (close < 0)
                {
                    AppendAsciiWithMacros(buffer, input.AsSpan(open));
                    break;
                }

                var inner = input.Substring(open + 1, close - open - 1);

                if (TryParseHexLike(inner, out var hexBytes))
                {
                    buffer.AddRange(hexBytes);
                }
                else
                {
                    invalidHexSegmentCount++;
                    AppendAsciiWithMacros(buffer, input.AsSpan(open, close - open + 1));
                }

                i = close + 1;
            }

            return buffer.ToArray();
        }

        private static void AppendAsciiWithMacros(List<byte> buffer, ReadOnlySpan<char> text)
        {
            if (text.IsEmpty)
                return;

            // Preserve existing macro behavior within ASCII segments.
            string processed = text.ToString();
            processed = processed.Replace("<CR>", "\r");
            processed = processed.Replace("<LF>", "\n");
            processed = processed.Replace("<TAB>", "\t");
            processed = processed.Replace("<NULL>", "\0");

            buffer.AddRange(Encoding.ASCII.GetBytes(processed));
        }

        private static bool TryParseHexLike(string inner, out byte[] bytes)
        {
            // Accept common separators and prefixes inside <>:
            // - spaces
            // - commas
            // - dashes
            // - 0x
            // Examples: <0D 0A>, <0x0d,0x0a>, <0D-0A>
            var compact = Regex.Replace(inner, @"0x", string.Empty, RegexOptions.IgnoreCase);
            compact = Regex.Replace(compact, @"[^0-9A-Fa-f]", string.Empty);

            if (string.IsNullOrEmpty(compact) || (compact.Length % 2 != 0))
            {
                bytes = [];
                return false;
            }

            bytes = new byte[compact.Length / 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                int high = HexCharToValue(compact[i * 2]);
                int low = HexCharToValue(compact[i * 2 + 1]);
                bytes[i] = (byte)((high << 4) | low);
            }

            return true;
        }

        private static int HexCharToValue(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            throw new FormatException($"Invalid hex character: {c}");
        }
    }
}
