using CommunityToolkit.Mvvm.ComponentModel;
using System;
using Quintilink.Models;

namespace Quintilink.ViewModels
{
    public partial class StatisticsViewModel : ObservableObject
    {
        private readonly ConnectionStatistics _statistics;

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

            AverageMessageSizeReceived = _statistics.AverageMessageSizeReceived.ToString("F1");
            AverageMessageSizeSent = _statistics.AverageMessageSizeSent.ToString("F1");

            IsConnected = _statistics.ConnectionStartTime != null && _statistics.ConnectionEndTime == null;
        }
    }
}
