namespace ThirdPartySeamlessWallet.Service.Model
{
    public class TpSeamlessBonus
    {
        public int PromotionCategoryId { get; set; }
        public int TpId { get; set; }
        public string TransId { get; set; }
        public int GameType { get; set; }
        public int CustId { get; set; }
        public string UserName { get; set; }
        public int CurrencyId { get; set; }
        public int SiteId { get; set; }
        /// <summary>
        /// 紀錄實際使用的Bonus金額
        /// </summary>
        public decimal Stake { get; set; }
        public int BonusId { get; set; }
    }
    
    public class UseTpSeamlessBonus
    {
        public int CustId { get; set; }
        public int BonusId { get; set; }
        public decimal UseBonus { get; set; }
    }

}
