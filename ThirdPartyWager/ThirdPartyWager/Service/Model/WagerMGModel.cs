namespace ThirdPartyWager.Service.Model
{
    public class MGFailedTransactions
    {
        public string betId { get; set; }
        public string actionType { get; set; }
        public string playerId { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string txnId { get; set; }
        public string txnEventType { get; set; }
        public string contentCode { get; set; }
        public bool completed { get; set; }
    }
    public class MGUpdatebalanceRequest
    {
        public string txnType { get; set; }
        public string txnEventType { get; set; }
        public string playerId { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string txnId { get; set; }
        public long creationTime { get; set; }
        public string contentCode { get; set; }
        public string betId { get; set; }
        public string roundId { get; set; }
        public string deviceType { get; set; }
        public string platformType { get; set; }
        public bool completed { get; set; }
        public bool IsFailedTransaction { get; set; }
    }
}