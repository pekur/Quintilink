using System;
using System.Collections.Generic;

namespace Quintilink.Models
{
    /// <summary>
    /// Represents a bookmark in the log stream
    /// </summary>
    public class LogBookmark
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = string.Empty;
        public int LogEntryIndex { get; set; }
        public string LogEntryPreview { get; set; } = string.Empty;

        public LogBookmark()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Represents a byte range for highlighting
    /// </summary>
    public class ByteHighlightRange
    {
        public byte RangeStart { get; set; }
        public byte RangeEnd { get; set; }
        public string Color { get; set; } = "#FFFF00"; // Yellow default
        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;

        public bool ContainsByte(byte value)
        {
            return value >= RangeStart && value <= RangeEnd;
        }
    }

    /// <summary>
    /// Result of a hex comparison between two messages
    /// </summary>
    public class HexComparisonResult
    {
        public List<ByteDifference> Differences { get; set; } = new();
        public int TotalBytes { get; set; }
        public int DifferentBytes { get; set; }
        public double SimilarityPercentage => TotalBytes > 0 ? ((TotalBytes - DifferentBytes) * 100.0 / TotalBytes) : 0;
    }

    /// <summary>
    /// Represents a single byte difference in comparison
    /// </summary>
    public class ByteDifference
    {
        public int Position { get; set; }
        public byte? Byte1 { get; set; }
        public byte? Byte2 { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Search filter for hex patterns in log
    /// </summary>
    public class HexSearchFilter
    {
        public string Pattern { get; set; } = string.Empty;
        public bool UseRegex { get; set; }
        public bool CaseSensitive { get; set; }
        public SearchDirection Direction { get; set; } = SearchDirection.RX;
    }

    public enum SearchDirection
    {
        All,
        TX,
        RX,
        SYS,
        ERR
    }
}
