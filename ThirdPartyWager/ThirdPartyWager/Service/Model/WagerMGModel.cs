using System;

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
        public string channel { get; set; }
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
        public bool completed { get; set; }
        public string channel { get; set; }
        public bool IsFailedTransaction { get; set; }
    }



    public class MGPlayerGameAnalysis
    {
        public DateTime time { get; set; }
        public string playerId { get; set; }
        public string channel { get; set; }
        public string gameName { get; set; }
        public string gameCode { get; set; }
        public string platform { get; set; }
        public decimal income { get; set; }
        public decimal payout { get; set; }
        public decimal grossWin { get; set; }
        public int numOfBets { get; set; }
        public int numOfPayouts { get; set; }
        public decimal margin { get; set; }
        public decimal maxBet { get; set; }
        public decimal maxPayout { get; set; }
        public decimal avgBet { get; set; }
        public decimal avgPayout { get; set; }
        public decimal progressiveBetAmount { get; set; }
    }

    public class MGFortuneRewards
    {
        public int campaignId { get; set; }
        public string campaignName { get; set; }
        public DateTime startCampaignDate { get; set; }
        public DateTime endCampaignDate { get; set; }
        public string playerId { get; set; }
        public DateTime creditDate { get; set; }
        public string transactionId { get; set; }
        public decimal rewardAmount { get; set; }
        public decimal payout { get; set; }
        public string referenceTransactionId { get; set; }
    }


}