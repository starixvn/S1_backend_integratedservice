using System;
using System.Collections.Generic;

namespace ThirdPartySeamlessWallet.Extensions
{
    public static class DateTimeExtension
    {
        public static string GetPartitionKey(this DateTime partitionTime)
        {
            return partitionTime.ToString("yyyyMM");
        }

        public static IEnumerable<string> GetPartitionRange(this DateTime partitionTime, int partitionCheckDay)
        {
            var result = new List<string>();
            var end = partitionTime.GetPartitionKey();
            var start = partitionTime.AddDays(partitionCheckDay * -1);
            var st = start.GetPartitionKey();

            int i = 0;
            while (st != end)
            {
                st = start.AddMonths(i).GetPartitionKey();
                result.Add(st);
                i++;
            }
            return result;
        }
    }
}
