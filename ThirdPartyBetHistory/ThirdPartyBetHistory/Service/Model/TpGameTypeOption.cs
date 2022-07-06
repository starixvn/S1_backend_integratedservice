using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.Model
{
    public class TpGameTypeOption
    {
        public int Type { get; set; }
        public string TypeName { get; set; }
        public int TpId { get; set; }
        public string TpName { get; set; }
        public TpCategory CategoryId { get; set; }
    }
}
