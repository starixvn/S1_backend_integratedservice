namespace ThirdPartyWager.Service.Model
{
    public class WagerMPModel
    {
        public int s { get; set; }
        public string m { get; set; }
        public Info_MP d { get; set; }
    }

    public class Info_MP
    {
        public int code { get; set; }
        public BetList_MP list { get; set; }
        public int count { get; set; }
        public long start { get; set; }
        public long end { get; set; }
    }

    public class BetList_MP
    {
        public IList<string> GameID { get; set; }
        public IList<string> Accounts { get; set; }
        public IList<int> ServerID { get; set; }
        public IList<int> KindID { get; set; }
        public IList<long> TableID { get; set; }
        public IList<int> ChairID { get; set; }
        public IList<int> UserCount { get; set; }
        public IList<string> CardValue { get; set; }
        public IList<decimal> CellScore { get; set; }
        public IList<decimal> AllBet { get; set; }
        public IList<decimal> Profit { get; set; }
        public IList<decimal> Revenue { get; set; }
        public IList<string> GameStartTime { get; set; }
        public IList<string> GameEndTime { get; set; }
        public IList<int> ChannelID { get; set; }
        public IList<string> LineCode { get; set; }
        // public IList<string> CreateTime { get; set; }
    }

    public class Bet_MP
    {
        public string GameID { get; set; }
        public string Accounts { get; set; }
        public int ServerID { get; set; }
        public int KindID { get; set; }
        public long TableID { get; set; }
        public int ChairID { get; set; }
        public int UserCount { get; set; }
        public string CardValue { get; set; }
        public decimal CellScore { get; set; }
        public decimal AllBet { get; set; }
        public decimal Profit { get; set; }
        public decimal Revenue { get; set; }
        public DateTime GameStartTime { get; set; }
        public DateTime GameEndTime { get; set; }
        public int ChannelID { get; set; }
        public string LineCode { get; set; }
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
                return new DateTimeOffset(GameEndTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public int GameType { get; set; }

        public string TpUserName
        {
            get
            {
                return Accounts.Replace($"{ChannelID}_", "");
            }
        }
    }

    public class WagerMPTotal
    {
        public int wagerCount { get; set; }
    }

    public class CurrencyStats_MP
    {
        public int s { get; set; }
        public string m { get; set; }
        public CurrencyStatsData_MP d { get; set; }
    }

    public class CurrencyStatsData_MP
    {
        public int code { get; set; }
        public string msg { get; set; }
        public IList<CurrencyStatsItem_MP> Transactions { get; set; }
    }

    public class CurrencyStatsItem_MP
    {
        public int betCount { get; set; }
        public decimal totalBetAmount { get; set; }
        public decimal playerPL { get; set; }
        public decimal totalWinAmount { get; set; }
        public string currency { get; set; }
    }

    public class MPConfig
    {
        public string ApiUrl { get; set; }
        public string WagerUrl { get; set; }
        public string Agent { get; set; }
        public string Deskey { get; set; }
        public string Md5key { get; set; }
    }
}
