namespace ThirdPartyCore.Model.TpMerchantBalance.Response
{
    public class MerchantBalanceResponse
    {
        /// <summary>
        /// 商戶餘額，餘額不足會影響玩家轉帳
        /// </summary>
        public decimal MerchantBalance { get; set; }

        /// <summary>
        /// 商戶名稱
        /// </summary>
    
        public string MerchantName { get; set; }

        /// <summary>
        /// 幣別名稱
        /// </summary>
        public string CurrencyName { get; set; }
    }
}
