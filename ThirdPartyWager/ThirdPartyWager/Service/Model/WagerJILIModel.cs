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

    public class JILIConfig
    {
        public string ApiUrl { get; set; }
        public string GameUrl { get; set; }
        public string AgentId { get; set; }
        public string AgentKey { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public string FunGameUrl { get; set; }
        public string AgentId_Stream { get; set; }
        public string Password_Stream { get; set; }
        public string Username_Stream { get; set; }
    }

}
