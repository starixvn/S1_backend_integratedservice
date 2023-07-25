namespace ThirdPartySeamlessWallet.Service.Model
{
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
    
    public class ErrorRespIM
    {
        public int Code { get; set; }
        public string Message { get; set; }
    }
    
    public class BettingData_IM
    {
        public string ProductWallet { get; set; }
        public string SessionToken { get; set; }
        public List<Transactions_IM> Transactions { get; set; }
    }
    
    public class Transactions_IM
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
    
    public class TransactionsBase
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
    
    public class TransactionsSlot : TransactionsBase
    {
        public string RoundId { get; set; }
        public string Platform { get; set; }
    }
    
    public class TransactionsSport : TransactionsBase
    {
        public string BetId { get; set; }
        public int ActionId { get; set; }
    }
    
    public class TransactionsESport : TransactionsBase
    {
        public string BetId { get; set; }
        public int ActionId { get; set; }
        public int Product { get; set; }
    }
}

