namespace ThirdPartyWager.Service.Model
{
    public class WagerEVOModel
    {
        public string field { get; set; }
        public string error { get; set; }
        public string id { get; set; }
        public string gameProvider { get; set; }
        public DateTime startedAt { get; set; }
        public DateTime settledAt { get; set; }
        public string status { get; set; }
        public string gameType { get; set; }
        public dynamic table { get; set; }
        public dynamic dealer { get; set; }
        public string currency { get; set; }
        public dynamic participants { get; set; }
        public dynamic result { get; set; }
        public decimal wager { get; set; }
        public decimal payout { get; set; }
    }

    public class Participant_EVO
    {
        public string casinoId { get; set; }
        public string playerId { get; set; }
        public string screenName { get; set; }
        public string sessionId { get; set; }
        public string casinoSessionId { get; set; }
        public string currency { get; set; }
        public IList<BetList_EVO> bets { get; set; }
        public dynamic configOverlays { get; set; }
        public string playMode { get; set; }
        public string channel { get; set; }
        public string os { get; set; }
        public string device { get; set; }
    }

    public class BetList_EVO
    {
        public string code { get; set; }
        public decimal stake { get; set; }
        public decimal payout { get; set; }
        public DateTime placedOn { get; set; }
        public string description { get; set; } = "";
        public string transactionId { get; set; }
    }

    public class WagerRoundInfoTotal_EVO
    {
        public decimal WagerTotal { get; set; }
        public decimal PayoutTotal { get; set; }
    }

    public class WagerRoundInfo_EVO : WagerEVOModel
    {
        public string TableId { get; set; }
        public long startedUnix
        {
            get
            {
                return new DateTimeOffset(startedAt).ToUnixTimeSeconds();
            }

        }
        public long settledUnix
        {
            get
            {
                return new DateTimeOffset(settledAt).ToUnixTimeSeconds();
            }
        }
    }

    public class Wager_EVO
    {
        public string RoundId { get; set; }
        public string casinoId { get; set; }
        public string playerId { get; set; }
        public string screenName { get; set; }
        public string sessionId { get; set; }
        public string casinoSessionId { get; set; }
        public string currency { get; set; }
        public string code { get; set; }
        public decimal stake { get; set; }
        public decimal payout { get; set; }
        public DateTime placedOn { get; set; }
        public string description { get; set; } = "";
        public string transactionId { get; set; }
        //public dynamic configOverlays { get; set; }
        public string playMode { get; set; }
        public string channel { get; set; }
        //public string os { get; set; }
        //public string device { get; set; }
        public dynamic participants { get; set; }
        public long placedUnix
        {
            get
            {
                return new DateTimeOffset(placedOn).ToUnixTimeSeconds();
            }
        }
    }

    public class Report_EVO
    {
        public string uuid { get; set; }
        public DateTime timestamp { get; set; }
        public IList<ReportDetail_EVO> data { get; set; }
    }

    public class ReportDetail_EVO
    {
        public string date { get; set; }
        public string gameType { get; set; }
        public dynamic table { get; set; }
        public int roundCount { get; set; }
        public string currency { get; set; }
        public double wager { get; set; }
        public double payout { get; set; }
        public string gameSubType { get; set; }
    }

    public class NLCConfig
    {
        public string ApiUrl_Backup { get; set; }
        public string EcId { get; set; }
        public string ApiUrl { get; set; }
        public string Ua2Token { get; set; }
        public string AuthToken { get; set; }
        public string CasinoKey { get; set; }
        public string GameProvider { get; set; }
        public string GameHistoryApiToken { get; set; }
    }

    public class EVOConfig : NLCConfig
    {
    }

    public class NetentConfig : NLCConfig
    {
    }

    public class RedTigerConfig : NLCConfig
    {
    }
}
