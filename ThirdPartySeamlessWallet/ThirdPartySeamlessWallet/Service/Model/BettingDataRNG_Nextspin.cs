using Microsoft.AspNetCore.Mvc;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_Nextspin
    {
        public string acctId { get; set; }
        public string transferId { get; set; }
        public string currency { get; set; }
        public decimal amount { get; set; }
        public int type { get; set; }
        public string gameCode { get; set; }
        public string referenceId { get; set; }
        public string ticketId { get; set; }
        public string channel { get; set; }
        public string specialGameType { get; set; }
        public string count { get; set; }
        public string sequence { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
    }
    public class NextspinResult 
    {
        public int Result { get; set; }
        public string SQ { get; set; }
        public decimal AfterAmount { get; set; }    
    }
    public class NextspinHeader 
    {
        [FromHeader]
        public string API { get; set; }
        [FromHeader]
        public string Digest { get; set; }
    }
    public class NextspinCommonResponse
    {
        public string SerialNo { get; set; }
        public int Code { get; set; }
        public string Msg { get; set; }
        public string MerchantCode { get; set; }
    }
    public class NextspinCommonRequest
    {
        public string MerchantCode { get; set; }
        public string SerialNo { get; set; }
    }
    public class AuthorizeRequest 
    {
        public string AcctId { get; set; }
        public string Token { get; set; }
        public string Language { get; set; }
        public string GameCode { get; set; }
        public bool ForFun { get; set; }
        public string MerchantCode { get; set; }
        public string SerialNo { get; set; }
    }
    public class GetBalanceRequest
    {
        public string AcctId { get; set; }
        public string MerchantCode { get; set; }
        public string SerialNo { get; set; }
        public string GameCode { get; set; }
    }
    public class GetBalanceResponse : NextspinCommonResponse
    {
        public AcctInfo AcctInfo { get; set; }
    }
    public class AcctInfo
    {
        public string UserName { get; set; }
        public string Currency { get; set; }
        public string AcctId { get; set; }
        public decimal Balance { get; set; }
    }
    public class TransferRequest
    {
        public string MerchantCode { get; set; }
        public string SerialNo { get; set; }
        public string AcctId { get; set; }
        public string TransferId { get; set; }
        public string Currency { get; set; }
        public decimal Amount { get; set; }
        public NextspinTransferType Type { get; set; }
        public string GameCode { get; set; }
        public string ReferenceId { get; set; }
        public string TicketId { get; set; }
        public string Channel { get; set; }
    }

    public class TransferResponse : NextspinCommonResponse
    {
        public string TransferId { get; set; }
        public string MerchantTxId { get; set; }
        public string AcctId { get; set; }
        public decimal Balance { get; set; }
    }
    public enum NextspinTransferType 
    {
        PlaceBet = 1,
        CancelBet = 2,
        Payout = 4,
        JackpotPayout = 6,
        JackpotPayoutByMerchant = 7,
        JackpotPayoutByGameProvider = 8,
        TournamentPayout = 9,
        TournamentPayoutByMerchant = 10,
        TournamentPayoutByGameProvider = 11,
    }
}
