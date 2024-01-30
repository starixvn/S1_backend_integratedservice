namespace ThirdPartyWager.Service.Model
{
    #region Base
    
    public class NTConnectResponse
    {
        public string status { get; set; }
        public string message { get; set; }
        public List<Error> errors { get; set; }
    }
    public class Error
    {
        public string message { get; set; }
        public string description { get; set; }
        public string code { get; set; }
    }
    
    #endregion
    
    public class GetTokenResponse : NTConnectResponse
    {
        public string token { get; set; }
        public DateTime token_expire { get; set; }
    }
    
}

