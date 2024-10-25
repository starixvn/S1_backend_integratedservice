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
        public string roundCode { get; set; }
        public string gameCode { get; set; }
        public int betType { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public long timestamp { get; set; }
        public DateTime UTCBetTime { get; set; }
    }

    public class EdayBetRequestResponse
    {
        public decimal balance { get; set; }
    }
    
    #endregion
    
    #region DAL

    public class BettransLTEday : EdayBetRequest
    {
        public decimal betTotal { get; set; }
        public decimal winTotal { get; set; }
        public int ticketCount { get; set; }
        public int SettleFlag { get; set; }
    }
    
    #endregion
    
}