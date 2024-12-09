namespace ThirdPartySeamlessWallet.Extensions
{
    public static class DateTimeExtension
    {
        public static string GetPartitionKey(this DateTime partitionTime)
        {
            return partitionTime.ToString("yyyyMM");
        }

        public static IList<DateTime> GetTimeFrame(this DateTime partitionTime, int partitionCheckDay)
        {
            var timeFrame = new List<DateTime>
            {
                partitionTime
            };
            if (partitionTime.Day <= partitionCheckDay)
            {
                timeFrame.Add(partitionTime.AddMonths(-1));
            }
            return timeFrame;
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

        public static IEnumerable<string> GetMonthlyPartitionRange(this DateTime startDate, DateTime endDate)
        {
            var result = new List<string>();
            while (startDate.EarlierThan(endDate))
            {
                result.Add(startDate.GetMonthlyPartitionKey());
                startDate = startDate.AddMonths(1);
            }
            return result;
        }

        public static string GetMonthlyPartitionKey(this DateTime partitionTime)
        {
            return partitionTime.ToString("yyyyMM");
        }

        public static bool EarlierThan(this DateTime startMonth, DateTime endMonth)
        {
            var start = int.Parse(startMonth.ToString("yyyyMM"));
            var end = int.Parse(endMonth.ToString("yyyyMM"));
            return start <= end;
        }
    }
}
