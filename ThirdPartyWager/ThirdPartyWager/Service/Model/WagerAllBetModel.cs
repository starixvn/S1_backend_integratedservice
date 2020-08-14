using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
}
