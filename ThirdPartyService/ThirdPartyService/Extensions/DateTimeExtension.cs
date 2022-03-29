using System;

namespace ThirdPartyService.Extensions
{
    public static class DateTimeExtension
    {
        public static bool IsBetween(this DateTime dt, DateTime startDate, DateTime endDate, bool compareTime = true)
        {
            return compareTime ?
                dt >= startDate && dt <= endDate :
                dt.Date >= startDate.Date && dt.Date <= endDate.Date;
        }
    }
}