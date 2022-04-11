namespace ThirdPartySeamlessWallet.Service.Model
{
    public class TpRequestFail
    {
        public int Id { get; set; }
        public int Type { get; set; }
        public int TpId { get; set; }
        public int CategoryId { get; set; }
        public string Request { get; set; }
        public string ErrorMessage { get; set; }
    }
}