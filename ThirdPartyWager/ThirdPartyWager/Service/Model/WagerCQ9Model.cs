namespace ThirdPartyWager.Service.Model
{
    public class CurrencyStats_CQ9
    {
        public Data_CQ9 data { get; set; }
        public Status_CQ9 status { get; set; }
    }

    public class Data_CQ9
    {
        public int totalsize { get; set; }
        public IList<CurrencyStatsData_CQ9> data { get; set; }

    }
    public class Status_CQ9
    {
        public string code { get; set; }
        public string message { get; set; }
        public DateTime datetime { get; set; }
        public string traceCode { get; set; }

    }

    public class CurrencyStatsData_CQ9
    {
        public decimal bets { get; set; }
        public decimal validbet { get; set; }
        public decimal wins { get; set; }
        public decimal jackpots { get; set; }
        public string currency { get; set; }
        public int totalsignup { get; set; }
        public int totalplayers { get; set; }
        public int totallogin { get; set; }
        public int totalround { get; set; }
        public string datetime { get; set; }
        public decimal rakes { get; set; }
        public decimal roomfee { get; set; }
        public decimal donate { get; set; }
        public string gameType { get; set; }
    }
}
