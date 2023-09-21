namespace ThirdPartySeamlessWallet.Service.Model
{
    #region Base

    public class PegasusTokenInfo
    {
        public string Token { get; set; }
        public string TpUserName { get; set; }
        public string CustId { get; set; }
        public string GameId { get; set; }
    }
    
    public class Response_Pegasus<T>
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }

    public class ExtraInfo
    {
        public int GameType { get; set; }
    }
    
    public class ResultPegasus
    {
        public decimal Balance { get; set; }
        public string TransactionNo { get; set; }
    }
    
    #endregion

    #region Balance

    public class GetBalanceReq
    {
        public string SessionToken { get; set; }
    }
    
    public class GetBalanceResp
    {
        public int Code { get; set; }
        public PGSCustBalance Data { get; set; }
    }

    public class PGSCustBalance
    {
        public decimal Balance { get; set; }
    }
    
    #endregion
    
    #region Bet

    public class BettingRequest_Pegasus
    {
        public string PlayerUniqueID { get; set; }
        public string SessionToken { get; set; }
        public string GameID { get; set; }
        public decimal BetAmount { get; set; }
        public string TransactionNo { get; set; }
        public ExtraInfo ExtraInfo { get; set; }
    }
    
    public class BetTransaction_Pegasus
    {
        public string PlayerUniqueID { get; set; }
        public string GameID { get; set; }
        public decimal BetAmount { get; set; }
        public string TransactionNo { get; set; }
        public string ExtraInfo { get; set; }
    }
    
    #endregion

    #region Settle

    public class SettleRequest_Pegasus
    {
        public string TransactionNo { get; set; }
        public string GameID { get; set; }
        public decimal BetAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal PayoutAmount { get; set; }
        public decimal ValidAmount { get; set; }
        public DateTime SettlementTime { get; set; }
        public string PlayerUniqueID { get; set; }
        public string SessionToken { get; set; }
        public ExtraInfo ExtraInfo { get; set; }
    }
    
    public class SettleTransaction_Pegasus
    {
        public string TransactionNo { get; set; }
        public string GameID { get; set; }
        public decimal BetAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal PayoutAmount { get; set; }
        public decimal ValidAmount { get; set; }
        public DateTime SettlementTime { get; set; }
        public string PlayerUniqueID { get; set; }
        public string ExtraInfo { get; set; }
        public DateTime CreateTime { get; set; }
    }
    
    #endregion
}

