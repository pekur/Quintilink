using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Quintilink.Models;

namespace Quintilink.Services
{
    public class LogExportService : ILogExportService
    {
        public async Task<bool> ExportLogAsync(string filePath, IEnumerable<LogEntry> entries, LogExportFormat format)
        {
            try
            {
                var content = format switch
                {
                    LogExportFormat.Csv => GenerateCsv(entries),
                    LogExportFormat.Json => GenerateJson(entries),
                    LogExportFormat.PlainText => GeneratePlainText(entries),
                    _ => throw new ArgumentException($"Unsupported format: {format}")
                };

                await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetFileFilter()
        {
            return "CSV Files (*.csv)|*.csv|JSON Files (*.json)|*.json|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
        }

        public string GetDefaultExtension(LogExportFormat format)
        {
            return format switch
            {
                LogExportFormat.Csv => ".csv",
                LogExportFormat.Json => ".json",
                LogExportFormat.PlainText => ".txt",
                _ => ".txt"
            };
        }

        private string GenerateCsv(IEnumerable<LogEntry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine(LogEntry.GetCsvHeader());

            foreach (var entry in entries)
            {
                sb.AppendLine(entry.ToCsvLine());
            }

            return sb.ToString();
        }

        private string GenerateJson(IEnumerable<LogEntry> entries)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(entries, options);
        }

        private string GeneratePlainText(IEnumerable<LogEntry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine("Quintilink Log Export");
            sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total Entries: {entries.Count()}");
            sb.AppendLine("========================================");
            sb.AppendLine();

            foreach (var entry in entries)
            {
                var bookmarkTag = entry.IsBookmarked ? " [BOOKMARK]" : string.Empty;
                sb.AppendLine($"[{entry.Timestamp:HH:mm:ss.fff}] [{entry.Direction}]{bookmarkTag}");

                if (!string.IsNullOrEmpty(entry.HexData))
                {
                    sb.AppendLine($"  HEX  : {entry.HexData}");
                }

                if (!string.IsNullOrEmpty(entry.AsciiData))
                {
                    sb.AppendLine($"  ASCII: {entry.AsciiData}");
                }

                if (!string.IsNullOrEmpty(entry.Message))
                {
                    sb.AppendLine($"  {entry.Message}");
                }

                if (entry.ByteCount > 0)
                {
                    sb.AppendLine($"  Bytes: {entry.ByteCount}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
