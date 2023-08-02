namespace ThirdPartyWager.Service.Model
{
    public class ServerInfo
    {
        public int SiteId { set; get; }
        public string SiteName { set; get; }
        public string LangList { set; get; }
        public string CurrencyList { set; get; }
        public string DefLang { set; get; }
        public int DefCurrency { set; get; }
    }

    public class TpCurrency
    {
        public int CurrencyId { set; get; }
        public string TpCurrencyId { set; get; }
        public decimal ExchangeRate { set; get; }
        public int IsSubCurrency { set; get; }
    }

    public class CurrencyOption
    {
        public int CurrencyId { set; get; }
        public string IsoCode { set; get; }
    }

    public class SettleData
    {
        public string TransId { get; set; }
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinLostAmount { get; set; }
        public decimal? Revenue { get; set; }
        public string Account { get; set; }
        public long SettleTime { get; set; }
        public long TransTime { get; set; }
        public decimal ProgressiveBet { get; set; }
        public decimal ProgressiveWin { get; set; }
        public string GameId { get; set; } = string.Empty;
        public int TicketStatus { get; set; }
        public int TicketType { get; set; }
    }
}
