namespace CommonService.Service.Model
{
    public class Currency
    {
        public int CurrencyId { get; set; }
        public string IsoCode { get; set; }
        public string Note { get; set; }
        public decimal ActualRate { get; set; }
        public string TimeZone { get; set; }
        public int Sort { get; set; }
    }
}
