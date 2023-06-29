using System;

namespace ThirdPartyWager.Service.Library
{
    public static class ReportTime
    {
        public static TimeCast ConvertTo(DateTimeHelper dateTimeHelper, DateTime startTime, int timezone, int day)
        {
            var reportTime = dateTimeHelper.GetTimeByDay(1440, timezone, "yyyy-MM-dd HH:mm:ss", startTime);
            var cvTime = DateTimeOffset.Now.AddDays(day).ToOffset(new TimeSpan(timezone, 0, 0)).ToUnixTimeSeconds();
            if (reportTime.endUnix > cvTime)
            {
                reportTime = dateTimeHelper.GetTimeByDay(1440, timezone, "yyyy-MM-dd HH:mm:ss", startTime.AddDays(-1));
            }
            return reportTime;
        }
    }
}
