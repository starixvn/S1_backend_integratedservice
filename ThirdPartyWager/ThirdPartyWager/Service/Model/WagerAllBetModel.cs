namespace ThirdPartyWager.Service.Model
{
    public class WagerAllBetModel<T>
    {
        public string resultCode { get; set; }
        public string message { get; set; }
        public data<T> data { get; set; }
    }

    public class SettleCheckAllBetModel
    {
        public string resultCode { get; set; }
        public string message { get; set; }
        public BetAllBet data { get; set; }
    }

    public class data<T>
    {
        public string startDateTime { get; set; }

        public string endDateTime { get; set; }

        public int total { get; set; }

        public int pageNum { get; set; }

        public int pageSize { get; set; }

        public T[] list { get; set; }
    }

    public class BetAllBet
    {
        public int appType { get; set; }
        public decimal betAmount { get; set; }
        public long betNum { get; set; }
        public DateTime betTime { get; set; }
        public int betType { get; set; }
        public string player { get; set; }
        public int commission { get; set; }
        public string currency { get; set; }
        public decimal exchangeRate { get; set; }
        public string gameResult { get; set; }
        public string gameResult2 { get; set; }
        public DateTime gameRoundEndTime { get; set; }
        public long gameRoundId { get; set; }
        public DateTime gameRoundStartTime { get; set; }
        public int gameType { get; set; }
        public int status { get; set; }
        public string tableName { get; set; }
        public decimal validAmount { get; set; }
        public decimal winOrLossAmount { get; set; }
        public decimal deposit { get; set; }


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
        public int MainSiteId {  get; set; }
        public int SiteId { get; set; }
        public string TpCurrencyId { get; set; }
    }

    public class RewardAllBet
    {
        public string agent { get; set; }
        public decimal amount { get; set; }
        public string player { get; set; }
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

    public class AllBetTransferResponse
    {
        public int resultCode { get; set; }

        public string message { get; set; }

        public decimal balance { get; set; }

        public int version { get; set; }
    }


    public class RewardAllBetRequest
    {
        public decimal amount { get; set; }
        public string currency { get; set; }
        public List<RewardAllBet> details { get; set; }
        public bool isRetry { get; set; }
        public string player { get; set; }
        public long tranId { get; set; }
        public int type { get; set; }
    }

    public class UnsettleCheck
    {
        public string betNum { get; set; }
        public string client { get; set; }
    }

    public class CurrencyStats_AllBet
    {
        public string resultCode { get; set; }
        public string message { get; set; }
        public CurrencyStatsData_AllBet data { get; set; }
    }

    public class CurrencyStatsData_AllBet
    {
        public string startDateTime { get; set; }
        public string endDateTime { get; set; }
        public int count { get; set; }
        public decimal totalBetAmount { get; set; }
        public decimal totalWinOrLoss { get; set; }
        public int CurrencyId { get; set; }
    }
}
