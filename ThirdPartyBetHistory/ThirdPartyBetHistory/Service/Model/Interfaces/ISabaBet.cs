namespace ThirdPartyBetHistory.Service.Model.Interfaces
{
    public interface ISabaBet
    {
        decimal odds { get; set; }
        decimal stake { get; set; }
        public decimal? discount_stake { get; set; }
        public int? odds_type { get; set; }
        public int? bet_type { get; set; }
        public string ticket_status { get; set; }
        string tokenmoney { get; }
    }
}
