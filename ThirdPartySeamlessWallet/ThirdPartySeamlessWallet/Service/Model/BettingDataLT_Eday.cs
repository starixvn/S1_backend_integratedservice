using Newtonsoft.Json;

namespace ThirdPartySeamlessWallet.Service.Model
{
    #region Config
    
    public class EdayConfig
    {
        public string ApiUrl { get; set; }
        public string Operator { get; set; }
        public string ApiKey { get; set; }
        public string AgentName { get; set; }
    }
    
    #endregion
    
    
    public class EdayResponse<T>
    {
        public int resultCode { get; set; }
        public string message { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public T data { get; set; }
    }

    public class EdayErrorResponse
    {
        public string message { get; set; }
    }

    #region GetBalance
    
    public class EdayGetBalanceResponse
    {
        public decimal balance { get; set; }
    }
    
    #endregion
    
    #region BetRequest
    
    public class EdayBetRequest
    {
        public string txId { get; set; }
        public string token { get; set; }
        public string player { get; set; }
        public decimal amount { get; set; }
        public int billId { get; set; }
        public long timestamp { get; set; }
        public string roundCode { get; set; }
        public string gameCode { get; set; }
        public int betType { get; set; }
    }

    public class EdayBetRequestData
    {
        public string txId { get; set; }
        public string token { get; set; }
        public string player { get; set; }
        public decimal amount { get; set; }
        public string billId { get; set; }
        public long timestamp { get; set; }
        public string roundCode { get; set; }
        public string gameCode { get; set; }
        public int betType { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public DateTime UTCBetTime { get; set; }
    }

    public class EdayBetRequestResponse
    {
        public decimal balance { get; set; }
    }
    
    #endregion
    
    #region RoundUpdate
    
    public class EdayRoundUpdateRequest
    {
        public string txId { get; set; }
        public long timestamp { get; set; }
        public int eventId { get; set; }
        public string roundCode { get; set; }
        public string gameCode { get; set; }
        public int totalPage { get; set; }
        public int txPage { get; set; }
        public int itemCount { get; set; }
        public int txType { get; set; }
        public List<EdayRoundUpdateData> items { get; set; }
    }
    public class EdayRoundUpdateData
    {
        public string player { get; set; }
        public decimal bet { get; set; }
        public decimal win { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public int SettleFlag { get; set; }
    }
    
    public class EdayRoundUpdateResponse
    {
        public decimal balance { get; set; }
    }
    
    #endregion
    
    #region DAL

    public class BettransLTEdayDAL
    {
        public string txId { get; set; }
        public string token { get; set; }
        public string player { get; set; }
        public string billId { get; set; }
        public decimal amount { get; set; }
        public long timestamp { get; set; }
        public string roundCode { get; set; }
        public string gameCode { get; set; }
        public int betType { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public int SettleFlag { get; set; }
        public DateTime UTCBetTime { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }
    
    public class BettransLTEdayCancelBet
    {
        public string billId { get; set; }
        public string player { get; set; }
        public int TicketType { get; set; } 
        public int TicketStatus { get; set; }
        public int SettleFlag { get; set; }
    }

    public class BettransLTEdayRoundUpdateDAL
    {
        public string txId { get; set; }
        public string player { get; set; }
        public decimal bet { get; set; }
        public decimal win { get; set; }
        public int eventId { get; set; }
        public string roundCode { get; set; }
        public string gameCode { get; set; }
        public long timestamp { get; set; }
        public int txType { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public int SettleFlag { get; set; }
        public DateTime UTCSettleBetTime { get; set; }
    }
    
    public class BettransLTEdayRoundCancelBet
    {
        public string roundCode { get; set; }
        public string player { get; set; }
        public int TicketType { get; set; } 
        public int TicketStatus { get; set; }
        public int SettleFlag { get; set; }
    }

    public class BettransLTEdayPayoutRequest : EdayBetRequest
    {
        public new string billId { get; set; }
        public decimal betTotal { get; set; }
        public decimal winTotal { get; set; }
        public int eventId { get; set; }
        public int ticketCount { get; set; }
        public DateTime UTCBetTime { get; set; }
        public DateTime UTCSettleTime { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public int SettleFlag { get; set; }
    }
    
    #endregion
    
}