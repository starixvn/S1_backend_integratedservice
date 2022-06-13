using System;
using System.Collections.Generic;

namespace ThirdPartyWager.Service.Model
{
    public class ReportDataMega
    {
        public string code { get; set; }
        public string message { get; set; }
        public IList<ReportListMega> result { get; set; }
    }

    public class ReportListMega
    {
        public string TransId { get; set; }
        public string loginId { get; set; }
        public long userId { get; set; }
        public decimal bet { get; set; }
        public decimal win { get; set; }
        public decimal yield { get; set; }
        public long idx { get; set; }
        public string memo { get; set; }
        public string Currency { get; set; }
        public long StartUnix { get; set; }
        public long EndUnix { get; set; }
        public bool Redo { get; set; }
    }

    public class GameLogUrl
    {
        public string code { get; set; }
        public string message { get; set; }
        public string id { get; set; }
        public string result { get; set; }
        public string jsonrpc { get; set; }
    }

    //public class SettleDataMega : ReportListMega
    //{
    //    public long TransId { get; set; }
    //    public long StartAt { get; set; }
    //    public int AddSecond { get; set; }
    //}

    public class SettleDataMega
    {
        public string loginId { get; set; }
        public string Currency { get; set; }
        public long StartAt { get; set; }
        public int AddSecond { get; set; }
        public decimal bet { get; set; }
        public decimal win { get; set; }
        public DateTime StartTime { get; set; }

    }

    public class WagerMega
    {
        public string msg { get; set; }
        public bool success { get; set; }
        public int ViewTYPE { get; set; }
        public IList<BetMega> results { get; set; }
        public int total { get; set; }

    }

    public class BetMega
    {
        public string TransId { get; set; }
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
        public long id { get; set; }
        public string LoginId { get; set; }
        public string Currency { get; set; }
        public long CreateUnix
        {
            get
            {
                return new DateTimeOffset(CreateTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }


    public class SummaryMega
    {
        public string Account { get; set; }
        public decimal Stake { get; set; }
        public decimal WinLostAmount { get; set; }
    }
}
