using System;
using System.Collections.Generic;

namespace ThirdPartyWager.Extensions
{
    public static class DateTimeExtension
    {
        public static DateTime BeginningOfTheDay(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day);
        }

        public static List<(DateTimeOffset begin , DateTimeOffset end)> DateToTimeRange(this DateTime date, int timeZone, int rangeHour)
        {
            var result = new List<(DateTimeOffset begin, DateTimeOffset end)>();
            var baseDate =  new DateTimeOffset(date).ToOffset(new TimeSpan(timeZone, 0, 0));
            for (int i = 0; i < 24; i += rangeHour)
            {               
                var range = (baseDate.AddHours(i), baseDate.AddHours(i + rangeHour));
                result.Add(range);
            }
            return result;
        }

        public static (DateTimeOffset begin, DateTimeOffset end) DateToTimeRange(this DateTime date, int timeZone)
        {
            var baseDate = new DateTimeOffset(date).ToOffset(new TimeSpan(timeZone, 0, 0));
            return (baseDate, baseDate.AddDays(1).AddMilliseconds(-1));
        }
    }
}