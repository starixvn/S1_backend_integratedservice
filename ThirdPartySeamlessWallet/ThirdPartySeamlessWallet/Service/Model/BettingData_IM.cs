namespace ThirdPartySeamlessWallet.Service.Model
{
    public class ErrorRespIM
    {
        public int Code { get; set; }
        public string Message { get; set; }
    }

    public class SettleErrorRespIM
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public string TransactionId { get; set; }
    }

    #region Bet

    public class BettingRequest_IM
    {
        public string ProductWallet { get; set; }
        public string SessionToken { get; set; }
        public List<BetTransactions_IM> Transactions { get; set; }
    }

    public class BetTransactions_IM
    {
        public string PlayerId { get; set; }
        public string ProviderPlayerId { get; set; }
        public string Provider { get; set; }
        public string GameId { get; set; }
        public string GameName { get; set; }
        public string RoundId { get; set; }
        public string BetId { get; set; }
        public string TransactionId { get; set; }
        public int ActionId { get; set; }
        public string Type { get; set; }
        public string Currency { get; set; }
        public decimal Amount { get; set; }
        public string Platform { get; set; }
        public int Product { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    public class BetTransactionsBase
    {
        public string PlayerId { get; set; }
        public string ProviderPlayerId { get; set; }
        public string Provider { get; set; }
        public string GameId { get; set; }
        public string GameName { get; set; }
        public string TransactionId { get; set; }
        public string Currency { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    public class BetTransactionsSlot : BetTransactionsBase
    {
        public string RoundId { get; set; }
        public string Platform { get; set; }
    }

    public class BetTransactionsSport : BetTransactionsBase
    {
        public string BetId { get; set; }
        public int ActionId { get; set; }
    }

    public class BetTransactionsESport : BetTransactionsBase
    {
        public string BetId { get; set; }
        public int ActionId { get; set; }
        public int Product { get; set; }
    }

    public class BetResponse
    {
        public List<BetResultIM> Results { get; set; }
    }

    public class BetResultIM
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public string OperatorTransactionId { get; set; }
        public string TransactionId { get; set; }
        public decimal Balance { get; set; }
    }

    #endregion

    #region Settle

    public class SettleRequest_IM
    {
        public string ProductWallet { get; set; }
        public List<SettleTransactions_IM> Transactions { get; set; }
    }

    public class SettleTransactionsBase : SettleAmountDataIM
    {
        public string PlayerId { get; set; }
        public string Provider { get; set; }
        public string GameId { get; set; }
        public string GameName { get; set; }
        public string TransactionId { get; set; }
        public string Type { get; set; }
        public string Currency { get; set; }
        public decimal Amount { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    public class SettleTransactions_IM : SettleTransactionsBase
    {
        public string RoundId { get; set; }
        public List<string> RefTransactionId { get; set; }
        public IMSpecialGame SpecialGame { get; set; }
        public string BetId { get; set; }
        public int ActionId { get; set; }
        public string EventId { get; set; }
        public int Product { get; set; }
    }

    public class SettleTransactions_IMSlot : SettleTransactionsBase
    {
        public string RoundId { get; set; }
        public string RefTransactionId { get; set; }
        public string SpecialGame { get; set; }
    }

    public class SettleTransactions_IMSport : SettleTransactionsBase
    {
        public string BetId { get; set; }
        public int ActionId { get; set; }
        public string EventId { get; set; }
    }

    public class SettleTransactions_IMESport : SettleTransactionsBase
    {
        public string BetId { get; set; }
        public int ActionId { get; set; }
        public int Product { get; set; }
        public string EventId { get; set; }
    }

    public class IMSpecialGame
    {
        public string SgType { get; set; }
        public int SgCount { get; set; }
        public int SgSequence { get; set; }
    }

    public class SettleResponse
    {
        public List<object> Results { get; set; }
    }

    public class SettleResultIM
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public string OperatorTransactionId { get; set; }
        public string TransactionId { get; set; }
        public decimal Balance { get; set; }
    }

    #endregion

    #region Refund

    public class RefundRequest_Spribe
    {
        public List<RefundTransaction_Spribe> Transactions { get; set; }
    }

    public class RefundTransaction_Spribe
    {
        public string TransactionId { get; set; }
        public string RefTransactionId { get; set; }
        public string TransactionType { get; set; }
        public string PlayerId { get; set; }
        public string ProviderPlayerId { get; set; }
        public string Provider { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    #endregion

    #region SettleWorker

    public class SettleAmountDataIM
    {
        public decimal Stake { get; set; }
        public decimal? ValidBet { get; set; }
        public decimal WinLostAmount { get; set; }
        public decimal PayoutAmount { get; set; }
    }

    public class WagerDataIM
    {
        public string PlayerId { get; set; }
        public string TransId { get; set; }
        public decimal BetAmount { get; set; }
        public decimal StakeAmount { get; set; }
        public decimal WinLoss { get; set; }
        public string Status { get; set; }
        public int IsSettled { get; set; }
        public int IsCancelled { get; set; }
        public string BetTradeStatus { get; set; }
        public decimal BetTradeBuybackAmount { get; set; }
        //spribe
        public DateTime BetDate { get; set; }
        //Sport , E-Sport
        public DateTime WagerCreationDateTime { get; set; }
    }

    #endregion
}

