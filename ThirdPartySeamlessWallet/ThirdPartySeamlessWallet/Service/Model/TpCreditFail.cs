namespace ThirdPartySeamlessWallet.Service.Model
{
    public class TpCreditFail
    {
        public int Id { get; set; }
        public int Type { get; set; }
        public int TpId { get; set; }
        public string Request { get; set; }
        public string ErrorMessage { get; set; }
    }
}