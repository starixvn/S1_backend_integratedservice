namespace ThirdPartyBetHistory.Service.Model
{
    public class VgamingUnSettle
    {
        public long TicketKey { get; set; }
        public int SiteId { get; set; }
        public string TpUserName { get; set; }
        public string TransId { get; set; }
        public string TicketAction { get; set; }
    }
}