using ThirdPartyCore.Model;

namespace ThirdParty.NTConnect
{
    
    #region Base
    
    public class Error
    {
        public string message { get; set; }
        public string description { get; set; }
        public string code { get; set; }
    }

    public class NTConnectResponse
    {
        public string status { get; set; }
        public string message { get; set; }
        public List<Error> errors { get; set; }
    }
    
    
    #endregion 
    
    
    #region Config
        
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
        
        
    #region 93ConnectResnpose
        
    public class GetTokenResponse : NTConnectResponse
    {
        public string token { get; set; }
        public DateTime token_expire { get; set; }
    }

    public class GetPlayerSessionResponse : NTConnectResponse
    {
        public string username { get; set; }
        public string session { get; set; }
        public string test_account { get; set; }
        public int time { get; set; }
    }

    public class GetGameUrlResponse : NTConnectResponse
    {
        public string gameurl { get; set; }
        public string username { get; set; }
    }

    public class GetRegisterOrLogoutResponse : NTConnectResponse
    {
        public string username { get; set; }
    }

    public class GetPlayerCheckResponse : NTConnectResponse
    {
        public string username { get; set; }
        public string session { get; set; }
        public string balance { get; set; }
        public string test_account { get; set; }
    }
    
    #endregion
        
    #region GetBetDetail
        
        
        
    #endregion
    
    #region RequestModel
    
    
    #endregion
}