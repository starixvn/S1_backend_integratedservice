using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyWager.Service.Model
{
    public class WagerSAModel
    {
        public GetAllBetDetailsForTimeIntervalResponse GetAllBetDetailsForTimeIntervalResponse { get; set; }
        public APIResponse APIResponse { get; set; }
    }

    public class GetAllBetDetailsForTimeIntervalResponse
    {
        public int ErrorMsgId { get; set; }
        public string ErrorMsg { get; set; }
        public BetDetailList BetDetailList { get; set; }

    }

    public class BetDetailList
    {
        public IList<BetSA> BetDetail { get; set; }

    }

    public class BetSA
    {
        public DateTime BetTime { get; set; }
        public DateTime PayoutTime { get; set; }
        public string Username { get; set; }
        public int HostID { get; set; }
        public string GameID { get; set; }
        public int Round { get; set; }
        public int Set { get; set; }
        public long BetID { get; set; }
        public decimal BetAmount { get; set; }
        public decimal Rolling { get; set; }
        public decimal ResultAmount { get; set; }
        public decimal Balance { get; set; }
        public string GameType { get; set; }
        public int BetType { get; set; }
        public int BetSource { get; set; }
        public string Detail { get; set; }
        public long TransactionID { get; set; }
        public dynamic GameResult { get; set; }
        public bool State { get; set; }
        public long betUnix
        {
            get
            {
                return new DateTimeOffset(BetTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long payOutUnix
        {
            get
            {
                return new DateTimeOffset(PayoutTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class APIResponse
    {
        public int ErrorMsgId { get; set; }
        public string ErrorMsg { get; set; }
    }
}
