namespace ThirdPartyWager.Service.Model.KY
{
    public class WagerKYModel
    {
        public int s { get; set; }
        public string m { get; set; }
        public info d { get; set; }
    }

    public class info
    {
        public int code { get; set; }
        public BetListKY list { get; set; }
        public int count { get; set; }
        public long start { get; set; }
        public long end { get; set; }
    }

    public class BetListKY
    {
        public IList<string> GameID { get; set; }
        public IList<string> Accounts { get; set; }
        public IList<int> ServerID { get; set; }
        public IList<int> KindID { get; set; }
        public IList<long> TableID { get; set; }
        public IList<int?> ChairID { get; set; }
        public IList<int> UserCount { get; set; }
        public IList<decimal> CellScore { get; set; }
        public IList<decimal> AllBet { get; set; }
        public IList<decimal> Profit { get; set; }
        public IList<decimal> Revenue { get; set; }
        public IList<string> GameStartTime { get; set; }
        public IList<string> GameEndTime { get; set; }
        public IList<string> CardValue { get; set; }
        public IList<int> ChannelID { get; set; }
        public IList<string> LineCode { get; set; }
        public IList<string> Currency { get; set; }
        public IList<string> Language { get; set; }
    }

    public class BetKY
    {
        public string GameID { get; set; }
        public string Accounts { get; set; }
        public int ServerID { get; set; }
        public int KindID { get; set; }
        public long TableID { get; set; }
        public int ChairID { get; set; }
        public int UserCount { get; set; }
        public decimal CellScore { get; set; }
        public decimal AllBet { get; set; }
        public decimal Profit { get; set; }
        public decimal Revenue { get; set; }
        public DateTime GameStartTime { get; set; }
        public DateTime GameEndTime { get; set; }
        public string CardValue { get; set; }
        public int ChannelID { get; set; }
        public string LineCode { get; set; }
        public string Currency { get; set; }
        public string Language { get; set; }
        public int TpId { get; set; }
        public int TpGameType { get; set; }
        public long startUnix
        {
            get
            {
                return new DateTimeOffset(GameStartTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long endUnix
        {
            get
            {
                return new DateTimeOffset(GameEndTime, new TimeSpan(8,0,0)).ToUnixTimeSeconds();
            }
        }
    }

    public class WagerKYTotal
    {
        public int wagerCount { get; set; }
    }

    public class CurrencyData
    {
        public string Deskey { get; set; }
        public string Agent { get; set; }
        public string Md5key { get; set; }
    }

    public class KYConfig
    {
        public string WagerUrl { get; set; }
        public string ApiUrl { get; set; }
        public Dictionary<string, CurrencyData> CurrencyData { get; set; }
    }
}
