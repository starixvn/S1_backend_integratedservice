using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.Model
{
    public class UnSettle
    {
        public long SQ { get; set; }
        public int SiteId { get; set; }
        public string TransId { get; set; }
        public int GameType { get; set; }
        public SettlementType SettlementType { get; set; }
        public DateTime SettledTime { get; set; }
    }
}