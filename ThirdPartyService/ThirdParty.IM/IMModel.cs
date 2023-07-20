namespace ThirdParty.IM;

public partial class TpHelper
{
    public class RegisterModel
    {
        public string MerchantCode { get; set; }
        public string PlayerId { get; set; }
        public string Currency { get; set; }
        public string Password { get; set; }
    }
    
    public class LaunchGameModel
    {
        public string MerchantCode { get; set; }
        public string? PlayerId { get; set; }
        public string GameCode { get; set; }
        public string Language { get; set; }
        public string IpAddress { get; set; }
        public int ProductWallet { get; set; }
        public string LobbyURL { get; set; }
    }
    
    public class RegisterResponseModel
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public string Currency { get; set; }
    }
    
    public class LaunchGameResponseModel
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public string GameUrl { get; set; }
    }
    
    public enum ProductWallet
    {
        SPRIBE = 101,
        IMSB = 301,
        ESportsbull = 401
    }
}