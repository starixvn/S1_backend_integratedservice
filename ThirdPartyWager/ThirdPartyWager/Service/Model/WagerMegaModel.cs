using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyWager.Service.Model
{
    //public class WagerMegaModel
    //{
    //    public string code { get; set; }
    //    public string message { get; set; }
    //    public decimal currMoney { get; set; }
    //    public string fg { get; set; }
    //    public int getcount { get; set; }
    //    public int pageindex { get; set; }
    //    public int pagesize { get; set; }
    //    public IList<BetMega> result { get; set; }
    //    public int total { get; set; }
    //}

    //public class BetMega
    //{
    //    public decimal BeginBlance { get; set; }
    //    public int ClassID { get; set; }
    //    public DateTime CreateTime { get; set; }
    //    public decimal EndBlance { get; set; }
    //    public int GameID { get; set; }
    //    public string GameName { get; set; }
    //    public int LineNum { get; set; }
    //    public string LogDataStr { get; set; }
    //    public int LogDataType { get; set; }
    //    public int RoundNO { get; set; }
    //    public int Rownum { get; set; }
    //    public int TableID { get; set; }
    //    public decimal Win { get; set; }
    //    public decimal bet { get; set; }
    //    public int cday { get; set; }
    //    public int cno { get; set; }
    //    public int id { get; set; }
    //    public string uuid { get; set; }
    //    public string Currency { get; set; }
    //    public string Account { get; set; }
    //    public long createUnix
    //    {
    //        get
    //        {
    //            return new DateTimeOffset(CreateTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
    //        }
    //    }
    //}

    public class WagerMegaReport
    {
        public string code { get; set; }
        public string message { get; set; }
        public IList<ReportMega> result { get; set; }
    }

    public class ReportMega
    {
        public string loginId { get; set; }
        public int userId { get; set; }
        public decimal bet { get; set; }
        public decimal yield { get; set; }
        public decimal win { get; set; }
        public string Currency { get; set; }
    }


    public class WagerMegaTotal

    {
        public string Account { get; set; }
        public int wagerCount { get; set; }
    }
}
