using Newtonsoft.Json;

namespace ThirdPartySeamlessWallet.Service.Model
{
    #region Cofig
    
    public class NTConnectConfig : TpSiteConfig
    {
        public string SiteName { get; set; }
        public int SiteId { get; set; }
        public List<CurrencyList> CurrencyList { get; set; }
    }

    public class CurrencyList
    {
        public string CurrencyCode { get; set; }
        public string ClientKey { get; set; }
        public string SecretKey { get; set; }
    }
    
    #endregion
    
    #region BaseResponse

    public class NTCResponse<T>
    {
        public string status { get; set; }
        public T data { get; set; }
    }

    public class NTCErrorResponse
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ErrorMessage { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? ErrorCode { get; set; }
    }

    #endregion
    
    #region GetBalance

    public class NTCGetBalanceRequest
    {
        public string token { get; set; }
        public string username { get; set; }
    }

    public class NTCGetBalanceResponse : NTCErrorResponse
    {
        public string username { get; set; }
        public decimal balance { get; set; }
    }

    #endregion

    #region CheckBet

    public class NTCCheckBetRequest
    {
        public string username { get; set; }
        public string ref_no { get; set; }
    }

    public class NTCCheckBetResponse : NTCErrorResponse
    {
        public decimal stake { get; set; }
        public decimal win_amt { get; set; }
        public string status { get; set; }
        public DateTime bet_date { get; set; }
    }

    #endregion

    #region PlaceBet

    public class NTCPlaceBetRequest
    {
        public string game { get; set; }
        public string game_scn { get; set; }
        public string drawno { get; set; }
        public string bet_odd { get; set; }
        public string bet_typegroup { get; set; }
        public string bet_type { get; set; }
        public string bet_id { get; set; }
        public string token { get; set; }
        public string username { get; set; }
        public string ip { get; set; }
        public decimal stake { get; set; }
        public string betTime { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public DateTime LocalBetTime { get; set; }
    }

    public class NTCPlaceBetResponse : NTCErrorResponse
    {
        public string username { get; set; }
        public decimal balance { get; set; }
    }

    #endregion

    #region SettleBet

    public class NTCSettleBetRequest
    {
        public string game { get; set; }
        public string bet_id { get; set; }
        public decimal bet_amt { get; set; }
        public decimal win_amt { get; set; }
        public string username { get; set; }
        public string status { get; set; }
        public string settleBetTime { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
    }

    public class NTCSettleBetResponse : NTCErrorResponse
    {
        public string username { get; set; }
        public decimal balance { get; set; }
    }

    #endregion

    #region CancelBet

    public class NTCCancelBetRequest
    {
        public string game { get; set; }
        public string username { get; set; }
        public string bet_id { get; set; }
        public string status { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
    }

    public class NTCCancelBetResponse : NTCErrorResponse
    {
        public string game { get; set; }
        public string username { get; set; }
        public string bet_id { get; set; }
    }

    #endregion
    
    #region DAL

    public class BettransLT93Connect : NTCPlaceBetRequest
    {
        public decimal bet_amt { get; set; }
        public decimal win_amt { get; set; }
        public string status { get; set; }
    }
    
    #endregion
}