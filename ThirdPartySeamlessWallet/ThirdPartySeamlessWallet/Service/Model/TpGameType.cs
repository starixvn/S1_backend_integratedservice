using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class TpGameType
    {
        public GameType GameType { get; set; }
        public string TypeName { get; set; }
        public Tp TpId { get; set; }
        public TpCategory CategoryId { get; set; }
        public PromotionCategory PromoCategoryId { get; set; }
    }
}
