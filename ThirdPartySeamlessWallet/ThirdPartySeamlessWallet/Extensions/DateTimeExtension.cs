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

        /// <remarks>
        /// partitionCheckDay 為幾號時觸發往前抓一個月
        /// </remarks>
        public static IEnumerable<string> GetPartitionRange(this DateTime partitionTime, int partitionCheckDay)
        {
            var result = new List<string>
            {
                partitionTime.GetPartitionKey()
            };

            if (partitionTime.Day <= partitionCheckDay)
            {
                result.Add(partitionTime.AddMonths(-1).GetPartitionKey());
            }
            return result;
        }
    }
}
