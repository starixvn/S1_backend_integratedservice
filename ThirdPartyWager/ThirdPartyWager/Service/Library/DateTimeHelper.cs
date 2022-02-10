using System;

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
        public TimeCast GetTimeByMinute(int period, int utc, string format, DateTime timerTime, int utcMinute = 0)
        {
            var current = timerTime;

            var timeBase = new DateTime(
                current.Year,
                current.Month,
                current.Day,
                current.Hour,
                current.Minute,
                0
            );

            var dEndTime = timeBase.AddMinutes(period + 1).AddMilliseconds(-1);

            if (dEndTime > DateTime.Now)
            {
                dEndTime = new DateTime(
                   DateTime.Now.Year,
                   DateTime.Now.Month,
                   DateTime.Now.Day,
                   DateTime.Now.Hour,
                   DateTime.Now.Minute,
                   0
                );

                dEndTime.AddMilliseconds(-1);
            }

            var beginAt = new DateTimeOffset(timeBase);
            var endAt = new DateTimeOffset(dEndTime);
            var startTime = beginAt.ToOffset(new TimeSpan(utc, utcMinute, 0)).ToString(format);
            var endTime = endAt.ToOffset(new TimeSpan(utc, utcMinute, 0)).ToString(format);
            var startUnix = beginAt.ToUnixTimeSeconds();
            var endUnix = endAt.ToUnixTimeSeconds();

            var timeCast = new TimeCast
            {
                startTime = startTime,
                endTime = endTime,
                startUnix = startUnix,
                endUnix = endUnix
            };

            return timeCast;
        }
        public TimeCast GetTimeByHour(int period, int utc, string format, DateTime timerTime, int addEndSecond = -1)
        {
            var current = timerTime;

            var timeBase = new DateTime(
                current.Year,
                current.Month,
                current.Day,
                current.Hour,
                0,
                0
            );

            var beginAt = new DateTimeOffset(timeBase);
            var endAt = new DateTimeOffset(timeBase.AddMinutes(period).AddMilliseconds(addEndSecond));
            var startTime = beginAt.ToOffset(new TimeSpan(utc, 0, 0)).ToString(format);
            var endTime = endAt.ToOffset(new TimeSpan(utc, 0, 0)).ToString(format);
            var startUnix = beginAt.ToUnixTimeSeconds();
            var endUnix = endAt.ToUnixTimeSeconds();

            var timeCast = new TimeCast
            {
                startTime = startTime,
                endTime = endTime,
                startUnix = startUnix,
                endUnix = endUnix
            };

            return timeCast;
        }
        public TimeCast GetTimeByDay(int period, int utc, string format, DateTime timerTime)
        {
            var current = timerTime;

            var timeBase = new DateTime(
                current.Year,
                current.Month,
                current.Day,
                0,
                0,
                0
            );

            var beginAt = new DateTimeOffset(timeBase);
            var endAt = new DateTimeOffset(timeBase.AddMinutes(period).AddMilliseconds(-1));
            var startTime = beginAt.ToOffset(new TimeSpan(utc, 0, 0)).ToString(format);
            var endTime = endAt.ToOffset(new TimeSpan(utc, 0, 0)).ToString(format);
            var startUnix = beginAt.ToUnixTimeSeconds();
            var endUnix = endAt.ToUnixTimeSeconds();

            var timeCast = new TimeCast
            {
                startTime = startTime,
                endTime = endTime,
                startUnix = startUnix,
                endUnix = endUnix
            };

            return timeCast;
        }
    }
}
