namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingData_JILI
    {
        public string reqId { get; set; }
        public string token { get; set; }
        public string currency { get; set; }
        public int game { get; set; }
        public long round { get; set; }
        public long wagersTime { get; set; }
        public decimal betAmount { get; set; }
        public decimal winloseAmount { get; set; }
        public string userId { get; set; }
        public bool isFreeRound { get; set; }
        public long TransactionID { get; set; }
        public decimal turnover { get; set; }
        public decimal preserve { get; set; }
        public decimal jpContribute { get; set; }
        public decimal jpWin { get; set; }
        public long sessionId { get; set; }
        public int type { get; set; }
    }

    public class CancelBetData_JILI
    {
        public string reqId { get; set; }
        public string token { get; set; }
        public string currency { get; set; }
        public int game { get; set; }
        public long round { get; set; }
        public decimal betAmount { get; set; }
        public decimal winloseAmount { get; set; }
        public string userId { get; set; }
        public long sessionId { get; set; }
        public int type { get; set; }
        public decimal preserve { get; set; }
    }

    public class BettransData_JILI : BettingData_JILI
    {
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
    }

    public class SettleData_JILI : SettleData
    {
    }

    public class CheckBettransData_JILI : BettransData_JILI
    {
        public int WagerCount { get; set; }
    }

    public class CheckBettingData_JILI
    {
        public long SQ { get; set; }
        public long TransId { get; set; }
        public string reqId { get; set; }
        public long round { get; set; }
        public string userId { get; set; }
        public string currency { get; set; }
        public int game { get; set; }
        public long wagersTime { get; set; }
        public decimal betAmount { get; set; }
        public decimal winloseAmount { get; set; }
        public decimal turnover { get; set; }
        public decimal preserve { get; set; }
        public decimal jpContribute { get; set; }
        public decimal jpWin { get; set; }
        public bool isFreeRound { get; set; }
        public long TransactionID { get; set; }
        public long sessionId { get; set; }
        public int type { get; set; }
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public int CustId { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public int SettleFlag { get; set; }
    }

    public class TpBetFailureRecordInfo_JILI
    {
        public decimal CurrentBalance { get; set; }
        public decimal betAmount { get; set; }
        public decimal winloseAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal InsufficientBalance { get; set; }
    }

    public class JILIConfig : TpSiteConfig
    {
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
