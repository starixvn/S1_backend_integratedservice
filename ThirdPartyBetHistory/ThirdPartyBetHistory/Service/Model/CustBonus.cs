namespace ThirdPartyBetHistory.Service.Model
{
    public class CustBonus
    {
        public int BonusId { get; set; }
        public decimal Bonus { get; set; }
        public int PromoCategoryId { get; set; }
        public int GameType { get; set; }
        public int TpId { get; set; }
    }
}
