namespace ThirdPartyBetHistory.Service.Model
{
    public class TpCustInfo
    {
        public int CustId { set; get; }
        public int CurrencyId { set; get; }
        public string UserName { set; get; }
        public string TpUserName { get; set; }
        public string TpPwd { set; get; }
        public int SiteId { set; get; }
        public string TpCustId { get; set; }
        public string TpCurrencyId { get; set; }
        public int MemberType { get; set; }
    }
}
