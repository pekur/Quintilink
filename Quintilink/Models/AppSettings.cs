using System.IO;
using System.IO.Ports;
using System.Text.Json;

namespace Quintilink.Models
{
    public class AppSettings
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 9000;
   
        // Serial Port Settings
        public string SerialPortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
        public int Parity { get; set; } = 0; // None = 0
        public int DataBits { get; set; } = 8;
        public int StopBits { get; set; } = 1; // One = 1

        public List<string> QuickSendHistory { get; set; } = new();
        public List<string> QuickSendPinnedSnippets { get; set; } = new();

        // Main window placement
        public double? MainWindowLeft { get; set; }
        public double? MainWindowTop { get; set; }
        public double? MainWindowWidth { get; set; }
        public double? MainWindowHeight { get; set; }
        public bool MainWindowMaximized { get; set; }

        private static readonly string FilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static AppSettings Load()
        {
            if (!File.Exists(FilePath))
                return new AppSettings();

            try
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(FilePath, json);
        }
    }
}
