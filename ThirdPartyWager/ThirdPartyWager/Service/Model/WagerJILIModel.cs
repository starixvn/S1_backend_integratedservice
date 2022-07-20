using System.Collections.Generic;

namespace ThirdPartyWager.Service.Model
{
    public class CurrencyStats_JILI
    {
        public int ErrorCode { get; set; }
        public string Message { get; set; }
        public IList<CurrencyStatsData_JILI> Data { get; set; }
    }

    public class CurrencyStatsData_JILI
    {
        public decimal BetAmount { get; set; }
        public decimal PayoffAmount { get; set; }
        public decimal Turnover { get; set; }
        public int Preserve { get; set; }
        public int WagersCount { get; set; }
        public string Currency { get; set; }

    }
    
}

