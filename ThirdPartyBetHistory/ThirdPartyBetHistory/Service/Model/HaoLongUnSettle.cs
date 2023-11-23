namespace ThirdPartyBetHistory.Service.Model
{
    public class HaoLongUnSettle
    {
        public long TicketKey { get; set; }
        public int SiteId { get; set; }
        public string TransId { get; set; }
        public string TpUserName { get; set; }
        public int drawType { get; set; }
        public DateTime drawDate { get; set; }
        public int SettleFlag { get; set; }
    }
}