using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataSports_SBO
    {
        
    }
    
    public class SBOBalanceRequest
    {
        public string CompanyKey { get; set; }
        public string Username { get; set; }
        public SBOProductType ProductType { get; set; }
        public int GameType { get; set; }
        public int Gpid { get; set; }
    }
    
    public class SBOBalanceResponse
    {
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}