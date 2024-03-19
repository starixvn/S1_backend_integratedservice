using ThirdPartyCore.Model;

namespace ThirdParty.WonCasino
{
    #region Base

    public class WonBaseResult
    {
        public int result { get; set; }
        public string desc { get; set; }
    }
    
    #endregion
    
    
    #region Config
    public class WonCasinoConfig : TpSiteConfig
    {
        public string SiteName { get; set; }

        public int SiteId { get; set; }
        public List<CurrencyList> CurrencyList { get; set; }

    }

    public class CurrencyList
    {
        public string CurrencyCode { get; set; }
        public string AppId { get; set; }
        public string SecretKey { get; set; }
    }
    #endregion
    
    #region login&create
    
    public class WonLoginResponse : WonBaseResult
    {
        public string openurl { get; set; }
    }
    
    #endregion
}