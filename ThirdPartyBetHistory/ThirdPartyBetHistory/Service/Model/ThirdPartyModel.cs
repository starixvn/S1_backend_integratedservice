using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyBetHistory.Service.Model
{
    public class ThirdPartyModel
    {
        public int Id { set; get; }
        public int SiteId { set; get; }
        public string TpName { set; get; }
        public string Setting { set; get; }
        public Dictionary<string, string> TpSetting => JsonConvert.DeserializeObject<Dictionary<string, string>>(Setting);
        public long TicketKey { set; get; }
        public int Status { set; get; }
    }
    public class TpCustInfo
    {
        public int CustId { set; get; }
        public int CurrencyId { set; get; }
        public string UserName { set; get; }
        public int SiteId { set; get; }
    }
    public class TpCurrency
    {
        public int CurrencyId { set; get; }
        public string TpCurrencyId { set; get; }
        public decimal ExchangeRate { set; get; }
    }
}
