using System.Collections.Generic;
using System.Threading.Tasks;
using Quintilink.Models;

namespace Quintilink.Services
{
    public enum LogExportFormat
    {
        Csv,
        Json,
        PlainText
    }

    public interface ILogExportService
    {
        /// <summary>
        /// Export log entries to a file
        /// </summary>
        Task<bool> ExportLogAsync(string filePath, IEnumerable<LogEntry> entries, LogExportFormat format);

        /// <summary>
        /// Get a file filter string for save dialog
        /// </summary>
        string GetFileFilter();

        /// <summary>
        /// Get default file extension for a format
        /// </summary>
        string GetDefaultExtension(LogExportFormat format);
    }
}
