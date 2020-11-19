using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyWager.Service.Model
{
    public class WagerKiss918Model
    {
        public int ViewTYPE { get; set; }
        public int code { get; set; }
        public decimal currMoney { get; set; }
        public string fg { get; set; }
        public int getcount { get; set; }
        public string msg { get; set; }
        public int pageindex { get; set; }
        public int pagesize { get; set; }
        public IList<BetKiss918> results { get; set; }
        public bool success { get; set; }
        public int total { get; set; }
    }

    public class BetKiss918
    {
        public decimal BeginBlance { get; set; }
        public int ClassID { get; set; }
        public DateTime CreateTime { get; set; }
        public decimal EndBlance { get; set; }
        public int GameID { get; set; }
        public string GameName { get; set; }
        public int LineNum { get; set; }
        public string LogDataStr { get; set; }
        public int LogDataType { get; set; }
        public int RoundNO { get; set; }
        public int Rownum { get; set; }
        public int TableID { get; set; }
        public decimal Win { get; set; }
        public decimal bet { get; set; }
        public int cday { get; set; }
        public int cno { get; set; }
        public int id { get; set; }
        public string uuid { get; set; }
        public string Currency { get; set; }
        public string Account { get; set; }
        public long createUnix
        {
            get
            {
                return new DateTimeOffset(CreateTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class WagerKiss918ReportPlayer
    {
        public int ViewTYPE { get; set; }
        public string msg { get; set; }
        public IList<ReportPlayerKiss918> results { get; set; }
        public bool success { get; set; }

    }

    public class ReportPlayerKiss918
    {
        public string Account { get; set; }
        public string win { get; set; }
    }


    public class WagerKiss918Total

    {
        public string Account { get; set; }
        public int wagerCount { get; set; }
    }
}
