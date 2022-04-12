using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_MG
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
        public string TransType { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public int SettleFlag { get; set; }
        public long trx_id { get; set; }
    }

    public class CancelDataRNG_MG
    {
        public decimal amount { get; set; }
        public string playerId { get; set; }
        public string txnId { get; set; }
        public string betId { get; set; }
        public long creationTime { get; set; }
    }

    public class MGLoginRequest
    {
        public string playerId { get; set; }
    }

    public class MGLoginResponse
    {
        public string currency { get; set; }
        public decimal balance { get; set; }
    }

    public class MGGetBalanceRequest
    {
        public string playerId { get; set; }
    }
    
    public class MGGetBalanceResponse
    {
        public string currency { get; set; }
        public decimal balance { get; set; }
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
        public string channel { get; set; }
    }

    public class MGUpdatebalanceResponse
    {
        public string extTxnId { get; set; }
        public decimal balance { get; set; }
        public string currency { get; set; }
    }
    
    public class MGRollbackeRequest
    {
        public string playerId { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string txnId { get; set; }
        public string betId { get; set; }
        public string txnEventType { get; set; }
        public string contentCode { get; set; }
        public string channel { get; set; }
        public long creationTime { get; set; }
    }

    public class MGRollbackResponse
    {
        public string extTxnId { get; set; }
        public decimal balance { get; set; }
        public string currency { get; set; }
    }

    public class BettransRng_MG
    {
        public long ExtTxnId { get; set; }
        public string PlayerId { get; set; }
        public decimal Amount { get; set; }
        public string RoundId { get; set; }
        public string TxnId { get; set; }
        public bool Completed { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
    }
    
    public class BettransLC_MG
    {
        public long ExtTxnId { get; set; }
        public string PlayerId { get; set; }
        public decimal Amount { get; set; }
        public string RoundId { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
    }

    public class MGPlaceBetResult
    {
        public int Result { get; set; }

        public long ExtTxnId { get; set; }

        public decimal AfterAmount { get; set; }
    }
    
    public class MGPayoutResult
    {
        public int Result { get; set; }

        public long ExtTxnId { get; set; }

        public decimal AfterAmount { get; set; }
    }
    
    public class MGCancelResult
    {
        public int Result { get; set; }

        public long ExtTxnId { get; set; }

        public decimal AfterAmount { get; set; }
    }
}