using System;

namespace Quintilink.Models
{
    public class ConnectionStatistics
    {
        public long BytesReceived { get; set; }
        public long BytesSent { get; set; }
        public int MessagesReceived { get; set; }
        public int MessagesSent { get; set; }
        public int ErrorCount { get; set; }
        public DateTime? ConnectionStartTime { get; set; }
        public DateTime? ConnectionEndTime { get; set; }

        public double PeakBytesPerSecondReceived { get; private set; }
        public double PeakBytesPerSecondSent { get; private set; }

        public TimeSpan ConnectionDuration
        {
            get
            {
                if (ConnectionStartTime == null)
                    return TimeSpan.Zero;

                var endTime = ConnectionEndTime ?? DateTime.Now;
                return endTime - ConnectionStartTime.Value;
            }
        }

        public double BytesPerSecondReceived
        {
            get
            {
                var duration = ConnectionDuration.TotalSeconds;
                return duration > 0 ? BytesReceived / duration : 0;
            }
        }

        public double BytesPerSecondSent
        {
            get
            {
                var duration = ConnectionDuration.TotalSeconds;
                return duration > 0 ? BytesSent / duration : 0;
            }
        }

        public long TotalBytes => BytesReceived + BytesSent;
        public int TotalMessages => MessagesReceived + MessagesSent;

        public double AverageMessageSizeReceived
        {
            get
            {
                return MessagesReceived > 0 ? (double)BytesReceived / MessagesReceived : 0;
            }
        }

        public double AverageMessageSizeSent
        {
            get
            {
                return MessagesSent > 0 ? (double)BytesSent / MessagesSent : 0;
            }
        }

        public void Reset()
        {
            BytesReceived = 0;
            BytesSent = 0;
            MessagesReceived = 0;
            MessagesSent = 0;
            ErrorCount = 0;
            ConnectionStartTime = null;
            ConnectionEndTime = null;
            PeakBytesPerSecondReceived = 0;
            PeakBytesPerSecondSent = 0;
        }

        public void StartConnection()
        {
            ConnectionStartTime = DateTime.Now;
            ConnectionEndTime = null;
        }

        public void EndConnection()
        {
            ConnectionEndTime = DateTime.Now;
        }

        public void RecordReceived(int byteCount)
        {
            BytesReceived += byteCount;
            MessagesReceived++;
            UpdatePeaks();
        }

        public void RecordSent(int byteCount)
        {
            BytesSent += byteCount;
            MessagesSent++;
            UpdatePeaks();
        }

        public void RecordError()
        {
            ErrorCount++;
        }

        private void UpdatePeaks()
        {
            if (BytesPerSecondReceived > PeakBytesPerSecondReceived)
                PeakBytesPerSecondReceived = BytesPerSecondReceived;

            if (BytesPerSecondSent > PeakBytesPerSecondSent)
                PeakBytesPerSecondSent = BytesPerSecondSent;
        }
    }
}
