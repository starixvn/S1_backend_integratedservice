namespace ThirdPartyCore.Model.TpBetRecordExtend
{
    public class GetTpBetRecordExtendRequest
    {
        public int CustId { get; set; }
        public int CurrencyId { get; set; }
        public BetRecordInfo BetRecordInfo { get; set; }
    }
}
