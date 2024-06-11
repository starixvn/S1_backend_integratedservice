namespace ThirdPartyWager.Service.Model
{
    public class CurrencyStats_AMB
    {
        public Status_AMB status { get; set; }
        public CurrencyStatsData_AMB data { get; set; }
    }

    public class Status_AMB
    {
        public int code { get; set; }
        public string message { get; set; }

    }

    public class CurrencyStatsData_AMB
    {
        public int totalRecords { get; set; }
        public int pageNumber { get; set; }
        public int currentRecords { get; set; }
        public IList<CurrencyStatsItem_AMB> response { get; set; }

    }

    public class CurrencyStatsItem_AMB
    {
        public string username { get; set; }
        public IList<CurrencyStatsItemList_AMB> result { get; set; }
    }

    public class CurrencyStatsItemList_AMB
    {
        public string _id { get; set; }
        public string username { get; set; }
        public string gameName { get; set; }
        public string gameId { get; set; }
        public string categories { get; set; }
        public string timestamp { get; set; }
        public string roundId { get; set; }
        public string roomId { get; set; }
        public string uuid { get; set; }
        public decimal bet { get; set; }
        public decimal turnover { get; set; }
        public decimal winlose { get; set; }
        public decimal commission { get; set; }
        public string TpCurrencyId { get; set; }
    }

    #region Wager

    public class Wager_AMB
    {
        public Status_AMB status { get; set; }
        public data_AMB data { get; set; }
    }
    public class data_AMB
    {
        public int totalRecords { get; set; }
        public int pageNumber { get; set; }
        public request_AMB request { get; set; }
        public List<userRecord_AMB> result { get; set; }
        public string path { get; set; }
    }

    public class request_AMB
    {
        public string agent { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
    }

    public class userRecord_AMB
    {
        public string username { get; set; }
        public List<betRecord_AMB> result { get; set; }
    }

    public class betRecord_AMB
    {
        public string _id { get; set; }
        public string username { get; set; }
        public string gameName { get; set; }
        public string gameId { get; set; }
        public string product { get; set; }
        public string categories { get; set; }
        public string timestamp { get; set; }
        public string roundId { get; set; }
        public string roomId { get; set; }
        public string uuid { get; set; }
        public decimal bet { get; set; }
        public decimal turnover { get; set; }
        public decimal winlose { get; set; }
        public decimal commission { get; set; }
        public bool isEndRound { get; set; }
        public bool isFreespin { get; set; }
        public bool isBuyFeature { get; set; }
        public bool isGamble { get; set; }
    }

    #endregion
}
