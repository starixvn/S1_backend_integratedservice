using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class CommDataLC_N2Live
    {
        public string userid { get; set; }
        public int vendorid { get; set; }
        public string timestamp { get; set; }
        public string sessiontoken { get; set; }
    }

    public class BettingDataLC_N2Live
    {
        public BetReq_N2Live request { get; set; }
    }

    public class BetReq_N2Live : CommDataLC_N2Live
    {
        public string gameid { get; set; }
        public string tradeid { get; set; }
        public decimal totalamount { get; set; }
        public string gamecode { get; set; }
        public BetTrades_N2Live trades { get; set; }
    }

    public class BetTrades_N2Live
    {
        public IList<Bet_N2Live> trade { get; set; }
    }

    public class Bet_N2Live
    {
        [JsonProperty("@id")]
        public string id { get; set; }
        public string area { get; set; }
        public decimal amount { get; set; }

        public Hand_N2Live? hand { get; set; }
    }

    public class Hand_N2Live
    {
        [JsonProperty("@parent")]
        public string? parent { get; set; }

        [JsonProperty("#text")]
        public decimal? text { get; set; }
    }

    public class CancelBetDataLC_N2Live
    {
        public CancelBetReq_N2Live request { get; set; }
    }

    public class CancelBetReq_N2Live : CommDataLC_N2Live
    {
        public string gameid { get; set; }
        public string tradeid { get; set; }
        public string gamecode { get; set; }
    }

    public class PayoutDataLC_N2Live
    {
        public PayoutReq_N2Live request { get; set; }
        public bool IsVoid { get; set; }
    }

    public class PayoutReq_N2Live : CommDataLC_N2Live
    {
        public string gameid { get; set; }
        public string tradeid { get; set; }
        public decimal totalamount { get; set; }
        public string gamecode { get; set; }
        public PayoutTrades_N2Live trades { get; set; }
    }

    public class PayoutTrades_N2Live
    {
        public IList<Payout_N2Live> trade { get; set; }
    }

    public class Payout_N2Live
    {
        [JsonProperty("@id")]
        public string id { get; set; }
        public string area { get; set; }
        public decimal amount { get; set; }
        public string resultstatus { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public dynamic? winningodds { get; set; }
        public Hand_N2Live? hand { get; set; }
    }

    public class BonusDataLC_N2Live
    {
        public BonusReq_N2Live request { get; set; }
    }

    public class BonusReq_N2Live : CommDataLC_N2Live
    {
        public string bonusid { get; set; }
        public string creditdate { get; set; }
        public decimal amount { get; set; }
        public string desc { get; set; }
    }

    public class EventDataLC_N2Live
    {
        public EventReq_N2Live request { get; set; }
    }

    public class EventReq_N2Live : CommDataLC_N2Live
    {
        public string eventtrxid { get; set; }
        public int eventtype { get; set; }
        public string trxtype { get; set; }
        public decimal amount { get; set; }
        public string desc { get; set; }
    }

    public class SmartBettingDataLC_N2Live
    {
        public SmartBetReq_N2Live request { get; set; }
    }

    public class SmartBetReq_N2Live : CommDataLC_N2Live
    {
        public string schemeid { get; set; }
        public decimal amount { get; set; }
        public string startdate { get; set; }
        public string enddate { get; set; }

        public long startUnix
        {
            get
            {
                return new DateTimeOffset(DateTime.Parse(startdate), new TimeSpan(0, 0, 0)).ToUnixTimeSeconds();
            }
        }

        public long endUnix
        {
            get
            {
                return new DateTimeOffset(DateTime.Parse(enddate), new TimeSpan(0, 0, 0)).ToUnixTimeSeconds();
            }
        }

        public long timeUnix
        {
            get
            {
                return new DateTimeOffset(DateTime.Parse(timestamp), new TimeSpan(0, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class SmartCancelBetDataLC_N2Live
    {
        public SmartCancelBetReq_N2Live request { get; set; }
    }

    public class SmartCancelBetReq_N2Live : CommDataLC_N2Live
    {
        public string schemeid { get; set; }
    }

    public class SmartPayoutDataLC_N2Live
    {
        public SmartPayoutReq_N2Live request { get; set; }
    }

    public class SmartPayoutReq_N2Live : CommDataLC_N2Live
    {
        public string schemeid { get; set; }
        public decimal amount { get; set; }
        public string reason { get; set; }

        public long timeUnix
        {
            get
            {
                return new DateTimeOffset(DateTime.Parse(timestamp), new TimeSpan(0, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class BettransData_N2Live
    {
        public long tradeid { get; set; }
        public decimal Stake { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public int SettleFlag { get; set; }
    }

    public class SettleData_N2Live : SettleData
    {
        public string betid { get; set; }
    }

    public class SettleData_N2Live_Smart : SettleData
    {
        public string schemeid { get; set; }
    }
}
