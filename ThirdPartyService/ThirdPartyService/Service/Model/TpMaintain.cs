using System;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Extensions;

namespace ThirdPartyService.Service.Model
{
    public class TpMaintain
    {
        public int SiteId { get; set; }
        public int TpId { get; set; }
        public int TpGameType { get; set; }
        public int TpCategoryId { get; set; }
        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }
        public MaintainType MaintainType { get; set; }
        public int TypeValue { get; set; }
        public string BeginTime { get; set; }
        public string EndTime { get; set; }

        public bool MaintainCheck(DateTime utcNow)
        {
            try
            {
                if (MaintainType == MaintainType.Date)
                {
                    return utcNow.IsBetween(BeginDate, EndDate);
                }

                // 檢查例行性維護是否還在有效區間內                          
                if (!utcNow.IsBetween(BeginDate, EndDate))
                {
                    return false;
                }

                // 將當下日期與維護時間組合後進行檢查
                var date = utcNow.Date.ToString("yyyy/MM/dd");
                var begin = DateTime.ParseExact($"{date} {BeginTime}", "yyyy/MM/dd HH:mm", null);
                var end = DateTime.ParseExact($"{date} {EndTime}", "yyyy/MM/dd HH:mm", null);
                var isBetween = utcNow.IsBetween(begin, end);

                return MaintainType switch
                {
                    MaintainType.Day => isBetween,
                    MaintainType.Week => isBetween && (int)utcNow.DayOfWeek == TypeValue,
                    MaintainType.Month => isBetween && utcNow.Day == TypeValue,
                    _ => false,
                };
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
