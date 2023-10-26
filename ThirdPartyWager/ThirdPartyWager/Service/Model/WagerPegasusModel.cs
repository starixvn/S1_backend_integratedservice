namespace ThirdPartyWager.Service.Model
{
    public class TpCurrencyConfig
    {
        public string Currency { get; set; }
        public string MerchantID { get; set; }
        public string SecretKey { get; set; }
        public string HeaderIdentity { get; set; }
    }
    
    public class WagerData_Pegasus<T>
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public string Currency { get; set; }
    }
    
    #region Wager
    
    public class PegasusData
    {
        public List<WagerDataDetail_Pegasus> Items { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPage { get; set; }
        public string ExtraObject { get; set; }
    }

    public class WagerDataDetail_Pegasus
    {
        public string TransactionNo { get; set; }
        public string PlayerName { get; set; }
        public int GameID { get; set; }
        public decimal WinLossAmount { get; set; }
        public string RoundID { get; set; }
        public DateTime BetTime { get; set; }
        public decimal BetAmount { get; set; }
        public decimal PayoutAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ValidAmount { get; set; }
        public ExtraInfo ExtraInfo { get; set; }
    }
    
    public class ExtraInfo
    {
        public int GameType { get; set; }
    }
    
    #endregion

    #region WagerSummary
    
    public class WagerSummary_Pegasus
    {
        public List<WagerSummaryDetail> GameSummary { get; set; }
        public decimal TotalPromotionBonus { get; set; }
        public int TotalRound { get; set; }
        public decimal TotalWinLossAmount { get; set; }
        public decimal TotalValidAmount { get; set; }
        public decimal TotalPayoutAmount { get; set; }
        public decimal TotalBetAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }
    }
    
    public class WagerSummaryDetail
    {
        public int GameID { get; set; }
        public int TotalRound { get; set; }
        public decimal TotalWinLossAmount { get; set; }
        public decimal TotalValidAmount { get; set; }
        public decimal TotalPayoutAmount { get; set; }
        public decimal TotalBetAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }
    }
    
    #endregion
}

