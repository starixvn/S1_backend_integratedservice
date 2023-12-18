namespace ThirdPartyCore.Model
{
    public class TpCurrency
    {
        public int CurrencyId { get; set; }
        public string TpCurrencyId { set; get; }
        public decimal ExchangeRate { set; get; }
        public int IsSubCurrency { get; set; }
    }

    public class TpCountryCode
    {
        public string CountryCode { set; get; }
        public int CurrencyId { set; get; }
        public string TpCurrencyId { set; get; }
    }

    public class DefTpCurrencyId
    {
        public int DefCurrency { set; get; }
        public string TpCurrencyId { set; get; }
    }

    public class CurrencyPrefix
    {
        public int CurrencyId { set; get; }
        public string PrefixCode { set; get; }
    }
}
