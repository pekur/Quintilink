using System;

namespace Quintilink.Models
{
    public class ConnectionStatistics
    {
        public DateTime ConnectionStartTime { get; set; }
        public DateTime? ConnectionEndTime { get; set; }
        
        public long BytesReceived { get; set; }
        public long BytesSent { get; set; }
        public long TotalBytes => BytesReceived + BytesSent;
        
        public int MessagesReceived { get; set; }
        public int MessagesSent { get; set; }
        public int TotalMessages => MessagesReceived + MessagesSent;
        
        public TimeSpan ConnectionDuration
        {
            get
            {
                var endTime = ConnectionEndTime ?? DateTime.Now;
                return endTime - ConnectionStartTime;
            }
        }
        
        public double BytesPerSecond
        {
            get
            {
                var duration = ConnectionDuration.TotalSeconds;
                return duration > 0 ? TotalBytes / duration : 0;
            }
        }
        
        public double MessagesPerSecond
        {
            get
            {
                var duration = ConnectionDuration.TotalSeconds;
                return duration > 0 ? TotalMessages / duration : 0;
            }
        }
        
        public double AverageBytesPerMessage
        {
            get
            {
                return TotalMessages > 0 ? (double)TotalBytes / TotalMessages : 0;
            }
        }

        public void Reset()
        {
            ConnectionStartTime = DateTime.Now;
            ConnectionEndTime = null;
            BytesReceived = 0;
            BytesSent = 0;
            MessagesReceived = 0;
            MessagesSent = 0;
        }

        public void RecordReceived(int byteCount)
        {
            BytesReceived += byteCount;
            MessagesReceived++;
        }

        public void RecordSent(int byteCount)
        {
            BytesSent += byteCount;
            MessagesSent++;
        }

        public void EndConnection()
        {
            ConnectionEndTime = DateTime.Now;
        }

        public string GetSummary()
        {
            return $"Duration: {ConnectionDuration:hh\\:mm\\:ss}\n" +
                   $"Total Bytes: {TotalBytes:N0} (RX: {BytesReceived:N0}, TX: {BytesSent:N0})\n" +
                   $"Total Messages: {TotalMessages:N0} (RX: {MessagesReceived:N0}, TX: {MessagesSent:N0})\n" +
                   $"Throughput: {BytesPerSecond:F2} bytes/sec\n" +
                   $"Message Rate: {MessagesPerSecond:F2} msg/sec\n" +
                   $"Avg Message Size: {AverageBytesPerMessage:F2} bytes";
        }
    }
}
