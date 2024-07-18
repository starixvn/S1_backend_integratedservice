namespace CommonService.Service.Model
{
    public class RequestObject
    {
        public int CustId { get; set; }
    }
    
    public class SignUpOTPRequest
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public string UserLang { get; set; }
        public string AreaCode { get; set; }
        public string PhoneNo { get; set; }
    }
}
