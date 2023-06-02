using Microsoft.AspNetCore.Mvc;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_Nextspin
    {
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
        public double Amount { get; set; }
        public int Type { get; set; }
        public string GameCode { get; set; }
        public string ReferenceId { get; set; }
        public string TicketId { get; set; }
        public string Channel { get; set; }
    }
}
