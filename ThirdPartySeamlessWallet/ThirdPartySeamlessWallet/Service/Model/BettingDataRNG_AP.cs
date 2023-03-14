using Newtonsoft.Json;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_AP
    {
        public string seq { get; set; }
        public string sitecode { get; set; }
        public string gamecode { get; set; } = null;
        public string gameroundid { get; set; }
        public decimal stake { get; set; } = 0;
        public decimal totalstake { get; set; } = 0;
        public decimal totalwin { get; set; } = 0;
        public decimal jpwin { get; set; } = 0;
        public string bettime { get; set; } = null;
        public string settletime { get; set; } = null;
        public string jpsettletime { get; set; } = null;
        public string ip { get; set; } = null;
        public string timestamp { get; set; }
        public string playerid { get; set; }
        public int hitjackpot { get; set; } = 0;
        public int TicketStatus { get; set; }
        public int TicketType { get; set; }
    }
    public class CancelBetDataRNG_AP
    {
        public string seq { get; set; }
        public string sitecode { get; set; }
        public string gamecode { get; set; }
        public string gameroundid { get; set; }
        public string timestamp { get; set; }
        public string playerid { get; set; }
        public decimal totalstake { get; set; }
    }
    public class BettransRNG_AP
    {
        public long SQ { get; set; }
        public int TicketStatus { get; set; }
    }
    public class SettleDataRNG_AP
    {
        public string seq { get; set; }
        public string sitecode { get; set; }
        public string gamecode { get; set; }
        public string gameroundid { get; set; }
        public decimal stake { get; set; } = 0;
        public decimal totalstake { get; set; } = 0;
        public decimal totalwin { get; set; } = 0;
        public decimal jpwin { get; set; } = 0;
        public long bettime { get; set; }
        public long settletime { get; set; }
        public long jpsettletime { get; set; }
        public string ip { get; set; } = null;
        public long timestamp { get; set; }
        public string playerid { get; set; }
        public int hitjackpot { get; set; } = 0;
        public int TicketStatus { get; set; }
        public int TicketType { get; set; }
    }
    public class CPSettleDataRNG_AP
    {
        public string seq { get; set; }
        public string sitecode { get; set; }
        public string playerid { get; set; }
        public string cpid { get; set; }
        public string cptransid { get; set; }
        public DateTime timestamp { get; set; }
        public decimal cpwin { get; set; }
        public long timeunix
        {
            get
            {
                return new DateTimeOffset(timestamp).ToUnixTimeSeconds();
            }
        }

    }

    public class BettingData_AdvantPlayMini
    {
        public string TransId
        {
            get
            {
                return $"{GameId}_{SessionId}";
            }
        }
        public string GameId { get; set; }
        public string SessionId { get; set; }
        public string UserReferenceId { get; set; }
        public decimal Amount { get; set; }
        public long BetTime { get; set; }
        public string MiniGameCode { get; set; }

        //[Description("SQ、transactionReferenceId")]
        //public string SQ { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class PayoutData_AdvantPlayMini
    {
        public string TransId
        {
            get
            {
                return TransactionReferenceId;
            }
        }

        public string TransactionReferenceId { get; set; }
        public string GameId { get; set; }
        public string SessionId { get; set; }
        public long SettlementTime { get; set; }
        public decimal StakeAmount { get; set; }
        public decimal WonAmount { get; set; }
        public decimal Multiplier { get; set; }
        public decimal Odds { get; set; }
        public IList<Data_AdvantPlayMini> Data { get; set; }
        public IList<WinResults_AdvantPlayMini> WinResults { get; set; } = new List<WinResults_AdvantPlayMini>();
        public IList<LostResults_AdvantPlayMini> LostResults { get; set; } = new List<LostResults_AdvantPlayMini>();
        public string GameResult { get; set; }
        public long RefundTime { get; set; }
        public string UserReferenceId { get; set; }
        public string MiniGameCode { get; set; }
    }

    public class WinResults_AdvantPlayMini
    {
        public string TransactionReferenceId { get; set; }
        public string UserReferenceId { get; set; }
        public decimal WonAmount { get; set; }
        public decimal BuyAmount { get; set; }
        public decimal Odds { get; set; }

    }

    public class LostResults_AdvantPlayMini
    {
        public string TransactionReferenceId { get; set; }
        public string Suit { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Data_AdvantPlayMini
    {
        public string TransactionReferenceId { get; set; }
        public decimal Amount { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ResultData_AdvantPlayMini
    {
        public string TransId
        {
            get
            {
                return $"{GameId}_{SessionId}";
            }
        }

        public string GameId { get; set; }
        public string SessionId { get; set; }
        public decimal Multiplier { get; set; }
        public IList<Data_AdvantPlayMini> Data { get; set; } = new List<Data_AdvantPlayMini>();
        public long SettlementTime { get; set; }
        public string UserReferenceId { get; set; }
        public string MiniGameCode { get; set; }
    }

    public class CancelBetData_AdvantPlayMini
    {
        public string TransId
        {
            get
            {
                return $"{GameId}_{SessionId}";
            }
        }

        public string GameId { get; set; }
        public string SessionId { get; set; }
        public long refundTime { get; set; }
        public IList<Data_AdvantPlayMini> Data { get; set; } = new List<Data_AdvantPlayMini>();
        public string UserReferenceId { get; set; }
        public string MiniGameCode { get; set; }
    }

    public class BettransData_AdvantPlayMini_Bet
    {
        public long SQ { get; set; }
        public string TransId { get; set; }
        public string GameId { get; set; }
        public long SessionId { get; set; }
        public string UserReferenceId { get; set; }
        public decimal Amount { get; set; }
        public long BetTime { get; set; }
        public string MiniGameCode { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
    }

    public class BettransData_AdvantPlayMini_Payout
    {
        public long SQ { get; set; }
        public string TransId { get; set; }
        public string TransactionReferenceId { get; set; }
        public string GameId { get; set; }
        public long SessionId { get; set; }
        public string UserReferenceId { get; set; }
        public long SettlementTime { get; set; }
        public decimal StakeAmount { get; set; }
        public decimal BuyAmount { get; set; }
        public decimal Multiplier { get; set; }
        public decimal WonAmount { get; set; }
        public decimal Odds { get; set; }
        public string Data { get; set; }
        public string WinResults { get; set; }
        public string LostResults { get; set; }
        public string GameResult { get; set; }
        public long RefundTime { get; set; }
        public string MiniGameCode { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
    }

    public class SettleData_AdvantPlayMini : SettleData
    {
        public string MiniGameCode { get; set; }
        public string status { get; set; }
        public long RefundTime { get; set; }
    }
}
