using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyWager.Service.Library
{

    public class TimeCast
    {
        public string startTime { get; set; }
        public string endTime { get; set; }
        public long startUnix { get; set; }
        public long endUnix { get; set; }
    }

    public class DateTimeHelper
    {
        public dynamic GetTimeByMinute(int begin, int end, int utc, string format, DateTime timerTime)
        {
            DateTime current = timerTime;
            if (timerTime >= DateTime.Now)
            {
                current = DateTime.Now.AddMinutes(-10);
            }
            DateTime timeBase = new DateTime(
                current.Year,
                current.Month,
                current.Day,
                current.Hour,
                current.Minute,
                0
            );

           
            var beginAt = new DateTimeOffset(timeBase.AddMinutes(begin));
            var endAt = new DateTimeOffset(timeBase.AddMinutes(end));

            var startTime = beginAt.ToOffset(new TimeSpan(utc, 0, 0)).ToString(format);
            var endTime = endAt.ToOffset(new TimeSpan(utc, 0, 0)).ToString(format);
            long startUnix = beginAt.ToOffset(new TimeSpan(utc, 0, 0)).ToUnixTimeSeconds();
            long endUnix = endAt.ToOffset(new TimeSpan(utc, 0, 0)).ToUnixTimeSeconds();

            var timeCast = new Dictionary<string, dynamic>
            {
                {"startTime", startTime},
                {"endTime", endTime},
                {"startUnix", startUnix},
                {"endUnix", endUnix}
            };

            return timeCast;
        }
    }
}
