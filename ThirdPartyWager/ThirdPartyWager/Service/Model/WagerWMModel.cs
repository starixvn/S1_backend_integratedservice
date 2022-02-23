using System;
using System.Collections.Generic;

namespace ThirdPartyWager.Service.Model
{
    public class WagerWMModel
    {
        public int errorCode { get; set; }
        public string errorMessage { get; set; }
        public IList<BetWM> result { get; set; }
    }

    public class BetWM
    {
        public string user { get; set; }
        public string betId { get; set; }
        public DateTime betTime { get; set; }
        public decimal bet { get; set; }
        public decimal validbet { get; set; }
        public decimal water { get; set; }
        public decimal result { get; set; }
        public string betCode { get; set; }
        public string betResult { get; set; }
        public decimal waterbet { get; set; }
        public decimal winLoss { get; set; }
        public string ip { get; set; }
        public int gid { get; set; }
        public int Event { get; set; }
        public int eventChild { get; set; }
        public int round { get; set; }
        public int subround { get; set; }
        public int tableId { get; set; }
        public string betwalletid { get; set; }
        public string resultwalletid { get; set; }
        public int commission { get; set; }
        public DateTime settime { get; set; }
        public string reset { get; set; }
        public string gameResult { get; set; }
        public string gname { get; set; }
        public string Currency { get; set; }
        public string TransId
        {
            get
            {
                return $"{gid}_{round}_{subround}_{user}";
            }
        }
        public long betUnix
        {
            get
            {
                return new DateTimeOffset(betTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long setUnix
        {
            get
            {
                return new DateTimeOffset(settime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class WagerWMTotal
    {
        public int wagerCount { get; set; }
    }
}
