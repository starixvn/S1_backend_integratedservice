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
    }
}