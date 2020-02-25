using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class TpCustInfo
    {
        public int CustId { set; get; }
        public int CurrencyId { set; get; }
        public string UserName { set; get; }
        public int SiteId { set; get; }
    }
    public class TpCustBalance
    {
        public decimal Balance { get; set; }
        public decimal ExchangeRate { get; set; }
        public string TpCurrencyId { get; set; }
    }
    public class TpCheck
    {
        public int CustStatus { get; set; }
        public int TpStatus { get; set; }
        public int TpCustStatus { get; set; }
        public string IP { get; set; }
        public string Token { get; set; }
        public bool Locked => CustStatus != 1 || TpStatus != 1 || TpCustStatus != 1;
    }
}
