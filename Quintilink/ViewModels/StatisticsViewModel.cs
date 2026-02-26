using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using Quintilink.Models;

namespace Quintilink.ViewModels
{
    public partial class StatisticsViewModel : ObservableObject
    {
        private readonly ConnectionStatistics _statistics;
        private const int MaxTrendPoints = 40;

        [ObservableProperty]
        private long bytesReceived;

        [ObservableProperty]
        private long bytesSent;

        [ObservableProperty]
        private long totalBytes;

        [ObservableProperty]
        private int messagesReceived;

        [ObservableProperty]
        private int messagesSent;

        [ObservableProperty]
        private int totalMessages;

        [ObservableProperty]
        private string connectionDuration = "00:00:00";

        [ObservableProperty]
        private string bytesPerSecondReceived = "0.0";

        [ObservableProperty]
        private string bytesPerSecondSent = "0.0";

        [ObservableProperty]
        private string averageMessageSizeReceived = "0.0";

        [ObservableProperty]
        private string averageMessageSizeSent = "0.0";

        [ObservableProperty]
        private bool isConnected;

        [ObservableProperty]
        private string bytesReceivedReadable = "0 B";

        [ObservableProperty]
        private string bytesSentReadable = "0 B";

        [ObservableProperty]
        private string totalBytesReadable = "0 B";

        [ObservableProperty]
        private string bytesPerSecondReceivedReadable = "0 B/s";

        [ObservableProperty]
        private string bytesPerSecondSentReadable = "0 B/s";

        [ObservableProperty]
        private string averageMessageSizeReceivedReadable = "0 B";

        [ObservableProperty]
        private string averageMessageSizeSentReadable = "0 B";

        [ObservableProperty]
        private string peakBytesPerSecondReceived = "0.0";

        [ObservableProperty]
        private string peakBytesPerSecondSent = "0.0";

        [ObservableProperty]
        private string peakBytesPerSecondReceivedReadable = "0 B/s";

        [ObservableProperty]
        private string peakBytesPerSecondSentReadable = "0 B/s";

        [ObservableProperty]
        private bool hasTraffic;

        [ObservableProperty]
        private string qualityStatus = "Idle";

        [ObservableProperty]
        private int errorCount;

        [ObservableProperty]
        private System.Windows.Media.PointCollection receivedTrendPoints = new();

        [ObservableProperty]
        private System.Windows.Media.PointCollection sentTrendPoints = new();

        public ObservableCollection<string> SessionSnapshots { get; } = new();

        public StatisticsViewModel(ConnectionStatistics statistics)
        {
            _statistics = statistics;
            UpdateStatistics();
        }

        public void UpdateStatistics()
        {
            BytesReceived = _statistics.BytesReceived;
            BytesSent = _statistics.BytesSent;
            TotalBytes = _statistics.TotalBytes;
            MessagesReceived = _statistics.MessagesReceived;
            MessagesSent = _statistics.MessagesSent;
            TotalMessages = _statistics.TotalMessages;

            var duration = _statistics.ConnectionDuration;
            ConnectionDuration = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";

            BytesPerSecondReceived = _statistics.BytesPerSecondReceived.ToString("F1");
            BytesPerSecondSent = _statistics.BytesPerSecondSent.ToString("F1");
            PeakBytesPerSecondReceived = _statistics.PeakBytesPerSecondReceived.ToString("F1");
            PeakBytesPerSecondSent = _statistics.PeakBytesPerSecondSent.ToString("F1");

            AverageMessageSizeReceived = _statistics.AverageMessageSizeReceived.ToString("F1");
            AverageMessageSizeSent = _statistics.AverageMessageSizeSent.ToString("F1");

            IsConnected = _statistics.ConnectionStartTime != null && _statistics.ConnectionEndTime == null;
            ErrorCount = _statistics.ErrorCount;

            BytesReceivedReadable = FormatBytes(BytesReceived);
            BytesSentReadable = FormatBytes(BytesSent);
            TotalBytesReadable = FormatBytes(TotalBytes);
            BytesPerSecondReceivedReadable = FormatBytesPerSecond(_statistics.BytesPerSecondReceived);
            BytesPerSecondSentReadable = FormatBytesPerSecond(_statistics.BytesPerSecondSent);
            PeakBytesPerSecondReceivedReadable = FormatBytesPerSecond(_statistics.PeakBytesPerSecondReceived);
            PeakBytesPerSecondSentReadable = FormatBytesPerSecond(_statistics.PeakBytesPerSecondSent);
            AverageMessageSizeReceivedReadable = FormatBytes(_statistics.AverageMessageSizeReceived);
            AverageMessageSizeSentReadable = FormatBytes(_statistics.AverageMessageSizeSent);

            HasTraffic = TotalMessages > 0;

            var qualityScore = TotalMessages == 0 ? 1.0 : Math.Max(0.0, 1.0 - ((double)ErrorCount / TotalMessages));
            QualityStatus = !HasTraffic
                ? "Idle"
                : qualityScore > 0.98
                    ? "Excellent"
                    : qualityScore > 0.9
                        ? "Good"
                        : "Warning";

            UpdateTrendPoints(_statistics.BytesPerSecondReceived, _statistics.BytesPerSecondSent);
        }

        [RelayCommand]
        private void ResetStatistics()
        {
            _statistics.Reset();
            if (IsConnected)
                _statistics.StartConnection();

            SessionSnapshots.Clear();
            ReceivedTrendPoints = new System.Windows.Media.PointCollection();
            SentTrendPoints = new System.Windows.Media.PointCollection();
            UpdateStatistics();
        }

        [RelayCommand]
        private void CaptureSnapshot()
        {
            SessionSnapshots.Insert(0, $"{DateTime.Now:HH:mm:ss} | Total {TotalBytesReadable} | RX {BytesReceivedReadable} | TX {BytesSentReadable}");
            while (SessionSnapshots.Count > 20)
                SessionSnapshots.RemoveAt(SessionSnapshots.Count - 1);
        }

        private void UpdateTrendPoints(double received, double sent)
        {
            var rxPoints = new System.Windows.Media.PointCollection(ReceivedTrendPoints);
            var txPoints = new System.Windows.Media.PointCollection(SentTrendPoints);

            if (rxPoints.Count >= MaxTrendPoints)
                rxPoints.RemoveAt(0);
            if (txPoints.Count >= MaxTrendPoints)
                txPoints.RemoveAt(0);

            rxPoints.Add(new Point(rxPoints.Count, received));
            txPoints.Add(new Point(txPoints.Count, sent));

            ReceivedTrendPoints = rxPoints;
            SentTrendPoints = txPoints;
        }

        private static string FormatBytes(double bytes)
        {
            string[] units = ["B", "KB", "MB", "GB", "TB"];
            var value = bytes;
            var unitIndex = 0;
            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return $"{value:F1} {units[unitIndex]}";
        }

        private static string FormatBytesPerSecond(double bytesPerSecond)
            => $"{FormatBytes(bytesPerSecond)}/s";
    }
}
