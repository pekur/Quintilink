using System.Text;

namespace Quintilink.Models
{
    public static class MacroDefinitions
    {
        // Macro -> byte(s) mapping (ASCII control names and common aliases)
        private static readonly Dictionary<string, byte[]> _macros = new(StringComparer.OrdinalIgnoreCase)
        {
            // C0 controls (0x00–0x1F)
            { "<NUL>", new byte[] { 0x00 } }, // Null
            { "<SOH>", new byte[] { 0x01 } }, // Start of Heading
            { "<STX>", new byte[] { 0x02 } }, // Start of Text
            { "<ETX>", new byte[] { 0x03 } }, // End of Text
            { "<EOT>", new byte[] { 0x04 } }, // End of Transmission
            { "<ENQ>", new byte[] { 0x05 } }, // Enquiry
            { "<ACK>", new byte[] { 0x06 } }, // Acknowledge
            { "<BEL>", new byte[] { 0x07 } }, // Bell
            { "<BELL>", new byte[] { 0x07 } },

            { "<BS>",  new byte[] { 0x08 } }, // Backspace

            { "<HT>",  new byte[] { 0x09 } }, // Horizontal Tab
            { "<TAB>", new byte[] { 0x09 } },

            { "<LF>",  new byte[] { 0x0A } }, // Line Feed
            { "<NL>",  new byte[] { 0x0A } }, // New Line

            { "<VT>",  new byte[] { 0x0B } }, // Vertical Tab
            { "<FF>",  new byte[] { 0x0C } }, // Form Feed

            { "<CR>",  new byte[] { 0x0D } }, // Carriage Return

            { "<SO>",  new byte[] { 0x0E } }, // Shift Out
            { "<SI>",  new byte[] { 0x0F } }, // Shift In

            { "<DLE>", new byte[] { 0x10 } }, // Data Link Escape
            { "<DC1>", new byte[] { 0x11 } }, // Device Control 1
            { "<XON>", new byte[] { 0x11 } }, // Common alias

            { "<DC2>", new byte[] { 0x12 } }, // Device Control 2
            { "<DC3>", new byte[] { 0x13 } }, // Device Control 3
            { "<XOFF>", new byte[] { 0x13 } }, // Common alias

            { "<DC4>", new byte[] { 0x14 } }, // Device Control 4
            { "<NAK>", new byte[] { 0x15 } }, // Negative Acknowledge
            { "<SYN>", new byte[] { 0x16 } }, // Synchronous Idle
            { "<ETB>", new byte[] { 0x17 } }, // End of Transmission Block
            { "<CAN>", new byte[] { 0x18 } }, // Cancel
            { "<EM>",  new byte[] { 0x19 } }, // End of Medium
            { "<SUB>", new byte[] { 0x1A } }, // Substitute
            { "<ESC>", new byte[] { 0x1B } }, // Escape
            { "<FS>",  new byte[] { 0x1C } }, // File Separator
            { "<GS>",  new byte[] { 0x1D } }, // Group Separator
            { "<RS>",  new byte[] { 0x1E } }, // Record Separator
            { "<US>",  new byte[] { 0x1F } }, // Unit Separator

            // Space (printable)
            { "<SP>",    new byte[] { 0x20 } },

            // DEL
            { "<DEL>", new byte[] { 0x7F } },
        };

        public static byte[] ExpandMacro(string macro)
        {
            return _macros.TryGetValue(macro, out var bytes) ? bytes : Array.Empty<byte>();
        }

        public static string CollapseByte(byte b)
        {
            foreach (var kvp in _macros)
            {
                if (kvp.Value.Length == 1 && kvp.Value[0] == b)
                {
                    return kvp.Key;
                }
            }

            // Printable ASCII
            if (b >= 0x20 && b <= 0x7E)
                return ((char)b).ToString();

            // Otherwise fallback
            return $"[{b:X2}]";
        }
    }
}
