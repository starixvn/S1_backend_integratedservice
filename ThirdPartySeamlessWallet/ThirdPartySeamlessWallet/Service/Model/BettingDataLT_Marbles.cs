using Newtonsoft.Json;

namespace ThirdPartySeamlessWallet.Service.Model
{
    #region Base
    
    public class MarblesConfig : TpSiteConfig
    {
        public string MerchantId { get; set; }
        public string MerchantCode { get; set; }
        public string AgentKey { get; set; }
    }
    public class MarblesRequest
    {
        [JsonProperty("params")]
        public string Params { get; set; }
    }
    public class VerifyRequest
    {
        public string traceId { get; set; }
        public string playerSession { get; set; }
    }
    public class MarblesResponse
    {
        public string code { get; set; }
        public string msg { get; set; }
    }

    public class VerifyResponse<T> : MarblesResponse
    {
        public T data { get; set; }
    }
    public class VerifyResponseData
    {
        public string traceId { get; set; }
        public string money { get; set; }
        public string account { get; set; }
    }
    
    #endregion 
    
    #region GetBalance
    
    public class MBSGetBalanceRequest
    {
        public string traceId { get; set; }
        public string lineCode { get; set; }
        public string agent { get; set; }
        public string account { get; set; }
    }
    public class MBSGetBalanceResponse : MarblesResponse
    {
        public MBSGetBalanceResponseData data { get; set; }
    }
    public class MBSGetBalanceResponseData
    {
        public string traceId { get; set; }
        public string money { get; set; }
        public string account { get; set; }
        public string currency { get; set; }
    }
    
    #endregion
    
    #region TransferInOut

    public class MBSTransferInOutRequest
    {
        public string traceId { get; set; }
        public int type { get; set; }
        public List<MBSBetSettleData> bets { get; set; }
    }

    public class MBSBetSettleData
    {
        public string agent { get; set; }
        public decimal ueffecBet { get; set; }
        public decimal ubet { get; set; }
        public decimal uprofit { get; set; }
        public string transNo { get; set; }
        public decimal bet { get; set; }
        public string lineCode { get; set; }
        public decimal effecBet { get; set; }
        public string gameCode { get; set; }
        public decimal commission { get; set; }
        public long startTime { get; set; }
        public string currency { get; set; }
        public long endTime { get; set; }
        public decimal profit { get; set; }
        public string gameNo { get; set; }
        public string account { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public int SettleFlag { get; set; }
    }

    public class MBSTransferInOutResponse : MarblesResponse
    {
        public string traceId { get; set; }
        public List<MBSResultBet> resultBets { get; set; }
    }
    
    public class MBSResultBet
    {
        public string transNo { get; set; }
        public decimal money { get; set; }
    }
    
    #endregion
    
    
    
}