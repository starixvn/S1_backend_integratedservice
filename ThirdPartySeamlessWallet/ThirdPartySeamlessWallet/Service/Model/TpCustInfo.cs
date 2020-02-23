using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class TpCustBalance
    {
        public decimal Balance { get; set; }
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
