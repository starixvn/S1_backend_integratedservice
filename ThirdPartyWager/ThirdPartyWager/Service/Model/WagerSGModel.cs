using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyWager.Service.Model
{
    public class WagerSGModel
    {
        public IList<BetSG> list { get; set; }
        public int resultCount { get; set; }
        public int pageCount { get; set; }
        public string merchantCode { get; set; }
        public string msg { get; set; }
        public int code { get; set; }
        public string serialNo { get; set; }
    }

    public class BetSG
    {
        public string ticketId { get; set; }
        public string acctId { get; set; }
        public string categoryId { get; set; }
        public string gameCode { get; set; }
        public string ticketTime { get; set; }
        public string betIp { get; set; }
        public string currency { get; set; }
        public decimal betAmount { get; set; }
        public decimal winLoss { get; set; }
        public decimal jackpotAmount { get; set; }
        public string result { get; set; }
        public bool completed { get; set; }
        public long luckyDrawId { get; set; }
        public int roundId { get; set; }
        public int sequence { get; set; }
        public string channel { get; set; }
        public decimal balance { get; set; }
        public decimal jpWin { get; set; }
        public string referenceId { get; set; }
        public long ticketUnix
        {
            get
            {
                DateTime oDate = DateTime.ParseExact($"{ticketTime}+08", "yyyyMMddTHHmmssfffzz", CultureInfo.InvariantCulture);
                return new DateTimeOffset(oDate).ToUnixTimeSeconds();
            }
        }
    }
}
