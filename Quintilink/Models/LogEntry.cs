using System;

namespace Quintilink.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Direction { get; set; } = string.Empty; // TX, RX, SYS, ERR
        public string HexData { get; set; } = string.Empty;
        public string AsciiData { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int ByteCount { get; set; }

        public LogEntry() { }

        public LogEntry(DateTime timestamp, string direction, string hexData, string asciiData, string message, int byteCount = 0)
        {
            Timestamp = timestamp;
            Direction = direction;
            HexData = hexData;
            AsciiData = asciiData;
            Message = message;
            ByteCount = byteCount;
        }

        public string ToCsvLine()
        {
            // Escape quotes in strings
            string escapedMessage = Message.Replace("\"", "\"\"");
            string escapedHex = HexData.Replace("\"", "\"\"");
            string escapedAscii = AsciiData.Replace("\"", "\"\"");

            return $"\"{Timestamp:yyyy-MM-dd HH:mm:ss.fff}\",\"{Direction}\",\"{escapedHex}\",\"{escapedAscii}\",\"{escapedMessage}\",{ByteCount}";
        }

        public static string GetCsvHeader()
        {
            return "Timestamp,Direction,Hex,ASCII,Message,ByteCount";
        }
    }
}
