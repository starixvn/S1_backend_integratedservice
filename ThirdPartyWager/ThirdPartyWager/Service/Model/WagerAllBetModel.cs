using System;
using System.Collections.Generic;

namespace ThirdPartyWager.Service.Model
{
    public class WagerAllBetModel
    {
        public DateTime endTime { get; set; }
        public string error_code { get; set; }
        public string message { get; set; }
        public Page page { get; set; }
        public DateTime startTime { get; set; }
    }

    public class Page
    {
        public int count { get; set; }
        public IList<BetAllBet> datas { get; set; }

    }

    public class BetAllBet
    {
        public int appType { get; set; }
        public decimal betAmount { get; set; }
        public long betNum { get; set; }
        public DateTime betTime { get; set; }
        public int betType { get; set; }
        public string client { get; set; }
        public int commission { get; set; }
        public string currency { get; set; }
        public decimal exchangeRate { get; set; }
        public string gameResult { get; set; }
        public string gameResult2 { get; set; }
        public DateTime gameRoundEndTime { get; set; }
        public long gameRoundId { get; set; }
        public DateTime gameRoundStartTime { get; set; }
        public int gameType { get; set; }
        public int state { get; set; }
        public string tableName { get; set; }
        public decimal validAmount { get; set; }
        public decimal winOrLoss { get; set; }

        public long betUnix
        {
            get
            {
                return new DateTimeOffset(betTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long roundStartUnix
        {
            get
            {
                return new DateTimeOffset(gameRoundStartTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }

        public long roundEndUnix
        {
            get
            {
                return new DateTimeOffset(gameRoundEndTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }

    }

    public class WagerAllBetTotal
    {
        public int wagerCount { get; set; }
    }

    public class RewardAllBetModel
    {
        public DateTime endTime { get; set; }
        public string error_code { get; set; }
        public string message { get; set; }
        public RewardPage page { get; set; }
        public DateTime startTime { get; set; }
    }

    public class RewardPage
    {
        public int count { get; set; }
        public IList<RewardAllBet> datas { get; set; }
    }

    public class RewardAllBet
    {
        public string agent { get; set; }
        public decimal amount { get; set; }
        public string client { get; set; }
        public string currency { get; set; }
        public string eventCode { get; set; }
        public long eventRecordNum { get; set; }
        public int eventType { get; set; }
        public decimal exchangeRate { get; set; }
        public DateTime settleTime { get; set; }
        public int status { get; set; }

        public long settleUnix
        {
            get
            {
                return new DateTimeOffset(settleTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class RewardAllBetData
    {
        public long tranId { get; set; }
        public int transferType { get; set; }
        public string client { get; set; }
        public string currency { get; set; }
        public decimal amount { get; set; }
        public long eventRecordNum { get; set; }
        public dynamic details { get; set; }
        public int SettleFlag { get; set; }
    }
}
