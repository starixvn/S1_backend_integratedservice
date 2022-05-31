using System;
using System.Collections.Generic;
using System.Text;

namespace ThirdPartyCore.Model
{
    public class TpCurrency
    {
        public string TpCurrencyId { set; get; }
        public decimal ExchangeRate { set; get; }
    }

    public class TpCountryCode
    {
        public string CountryCode { set; get; }
        public int CurrencyId { set; get; }
        public string TpCurrencyId { set; get; }
    }
}
