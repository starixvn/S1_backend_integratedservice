using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    #region Base

    public class PegasusTokenInfo
    {
        public string Token { get; set; }
        public string TpUserName { get; set; }
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
    
    public class EventDetailInfo
    {
        public int PlayerID { get; set; }
        public string PlayerName { get; set; }
        public int GameID { get; set; }
        public int EventID { get; set; }
        public int EventPrizeType { get; set; }
        public int Status { get; set; }
        public string Memo { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime ReceiveTime { get; set; }
        public int EventPrizeRecordID { get; set; }
        public string EventName { get; set; }
        public string EventPrizeTypeText { get; set; }
        public decimal QuantityOrReferenceRatio { get; set; }
        public decimal PrizeCashAmount { get; set; }
        public string StatusText { get; set; }
        public string PrizeRecordTransactionNo { get; set; }
        public string GameName { get; set; }
    }
    
    public class ResultPegasus
    {
        public decimal Balance { get; set; }
        public string TransactionNo { get; set; }
    }
    
    public class BonusResultPegasus
    {
        public string TransactionNo { get; set; }
        public string MerchantOrderNo { get; set; }
        public decimal BeforeAmount { get; set; }
        public decimal AfterAmount { get; set; }
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
        public TicketStatus TicketStatus { get; set; }
        public DateTime CreateTime { get; set; }
    }
    
    #endregion

    #region Cancel

    public class CancelRequest_Pegasus
    {
        public string TransactionNo { get; set; }
        public string GameID { get; set; }
        public string PlayerUniqueID { get; set; }
        public string SessionToken { get; set; }
    }

    #endregion

    #region Bonus

    public class BonusRequest_Pegasus
    {
        public string TransactionNo { get; set; }
        public decimal Amount { get; set; }
        public string PlayerUniqueID { get; set; }
        public EventDetailInfo eventDetailInfo { get; set; }
    }
    
    public class BonusTransaction_Pegasus
    {
        public string TransactionNo { get; set; }
        public decimal Amount { get; set; }
        public string PlayerUniqueID { get; set; }
        public string EventDetailInfo { get; set; }
    }

    #endregion

    #region SettleWorker

    public class WagerPegasus
    {
        public string TransactionNo { get; set; }
        public string PlayerName { get; set; }
        public string GameID { get; set; }
        public decimal WinLossAmount { get; set; }
        public string RoundID { get; set; }
        public DateTime BetTime { get; set; }
        public decimal BetAmount { get; set; }
        public decimal PayoutAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ValidAmount { get; set; }
        public string ExtraInfo { get; set; }
        public DateTime CreateTime { get; set; }
    }

    #endregion
}

