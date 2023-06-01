namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_Nextspin
    {
    }
    public class GetBalanceRequest
    {
        public string AcctId { get; set; }
        public string MerchantCode { get; set; }
        public string SerialNo { get; set; }
        public string GameCode { get; set; }
    }
    public class NextspinCommonResponse 
    {
        public string SerialNo { get; set; }
        public int Code { get; set; }
        public string Msg { get; set; }
        public string MerchantCode { get; set; }
    }
    public class GetBalanceResponse : NextspinCommonResponse
    {
        public AcctInfo AcctInfo { get; set; }
    }
    public class AcctInfo
    {
        public string UserName { get; set; }
        public string Currency { get; set; }
        public string AcctId { get; set; }
        public decimal Balance { get; set; }
    }
}
