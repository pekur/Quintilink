using System.Collections.Generic;
using System.Linq;
using Quintilink.Models;

namespace Quintilink.Services
{
    public interface IHexComparisonService
    {
        HexComparisonResult Compare(byte[] data1, byte[] data2);
        HexComparisonResult Compare(MessageDefinition msg1, MessageDefinition msg2);
    }

    public class HexComparisonService : IHexComparisonService
    {
        public HexComparisonResult Compare(byte[] data1, byte[] data2)
        {
            var result = new HexComparisonResult();
            var maxLength = Math.Max(data1.Length, data2.Length);
            result.TotalBytes = maxLength;

            for (int i = 0; i < maxLength; i++)
            {
                byte? b1 = i < data1.Length ? data1[i] : null;
                byte? b2 = i < data2.Length ? data2[i] : null;

                if (b1 != b2)
                {
                    result.DifferentBytes++;
                    result.Differences.Add(new ByteDifference
                    {
                        Position = i,
                        Byte1 = b1,
                        Byte2 = b2,
                        Description = GetDifferenceDescription(i, b1, b2)
                    });
                }
            }

            return result;
        }

        public HexComparisonResult Compare(MessageDefinition msg1, MessageDefinition msg2)
        {
            return Compare(msg1.GetBytes(), msg2.GetBytes());
        }

        private string GetDifferenceDescription(int position, byte? b1, byte? b2)
        {
            if (b1 == null)
                return $"Position {position}: Message 1 ended, Message 2 has {b2:X2}";
            if (b2 == null)
                return $"Position {position}: Message 1 has {b1:X2}, Message 2 ended";
            return $"Position {position}: {b1:X2} ? {b2:X2}";
        }
    }
}
