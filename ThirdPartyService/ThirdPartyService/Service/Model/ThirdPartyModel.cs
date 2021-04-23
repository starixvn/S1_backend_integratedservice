using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyService.Service.Model
{
    public class ThirdPartyModel
    {
        public int Id { set; get; }
        public string TpName { set; get; }
        public string Setting { set; get; }
        public int Status { set; get; }
        public int WalletType { set; get; }
    }
    public class TpCurrency
    {
        public string TpCurrencyId { set; get; }
        public decimal ExchangeRate { set; get; }
    }
}
