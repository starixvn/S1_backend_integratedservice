using Newtonsoft.Json;

namespace ThirdPartyBetHistory.Service.Model
{
    public class TpCustInfo
    {
        public int CustId { set; get; }
        public int CurrencyId { set; get; }
        public string UserName { set; get; }
        public int SiteId { set; get; }
    }
}
