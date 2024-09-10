using Newtonsoft.Json;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataLC_EBet
    {
    }

    public class RegisterLoginData_EBet
    {
        public string username { get; set; }
        public string password { get; set; }
        public int channelId { get; set; }
        public string currency { get; set; }
        public string ip { get; set; }
        public int eventType { get; set; }
        public string accessToken { get; set; }
        public int platform { get; set; }
        public string Event { get; set; }
        public long timestamp { get; set; }
        public string sessionToken { get; set; }
        public string seqNo { get; set; }
        public string signature { get; set; }
    }
    
    public class Validate_EBet
    {
        public int channelId { get; set; }
        public long timestamp { get; set; }
        public string signature { get; set; }
        public string Event { get; set; }
        public string sessionToken { get; set; }
        public string ip { get; set; }
        public string seqNo { get; set; }
    }

    public class SyncCreditData_EBet
    {
        public int channelId { get; set; }
        public string username { get; set; }
        public int tableType { get; set; }
        public string currency { get; set; }
        public string Event { get; set; }
        public long timestamp { get; set; }
        public string sessionToken { get; set; }
        public string seqNo { get; set; }
    }

    public class BettingData_EBet
    {
        public string username { get; set; }
        public int channelId { get; set; }
        public decimal money { get; set; }
        public int type { get; set; }
        public int platform { get; set; }
        public string currency { get; set; }
        public string seqNo { get; set; }
        public string Event { get; set; }
        public long timestamp { get; set; }
        public string sessionToken { get; set; }
        public string signature { get; set; }
        public DetailEbet detail { get; set; }
    }

    public class DetailEbet
    {
        public string tableCode { get; set; }
        public int tableType { get; set; }
        public int tableSubType { get; set; }
        public string roundCode { get; set; }
        public long betTime { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IList<int?> srcResults { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IList<int?> results { get; set; }
        public IList<BetListEbet> betList { get; set; }
        public decimal totalBet { get; set; }
        public decimal validBet { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? payout { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long? payoutTime { get; set; }
        public long betStopTime { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IList<WithHoldingListEbet> withHoldingList { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string withHoldingSeqNo { get; set; }
    }

    public class BetListEbet
    {
        public long betType { get; set; }
        public decimal betMoney { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string seqNo { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? payout { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? validBet { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string betId { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? odds { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string number { get; set; }
    }

    public class WithHoldingListEbet
    {
        public long betType { get; set; }

        public decimal withHolding { get; set; }

        public decimal refund { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string seqNo { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int status { get; set; } // 1: 成功、0: 失败或没有金额需要返还、11:退款、13:退款失败

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string withHoldingId { get; set; }
    }

    public class CreditRecordData_EBet
    {
        public int channelId { get; set; }
        public string username { get; set; }
        public string querySeqNo { get; set; }
        public string roundCode { get; set; }
        public string Event { get; set; }
        public long timestamp { get; set; }
        public string sessionToken { get; set; }
        public string seqNo { get; set; }
        public string signature { get; set; }
    }

    public class RefundSingleWallet_EBet
    {
        public int channelId { get; set; }
        public string username { get; set; }
        public string roundCode { get; set; }
        public string seqNo { get; set; }
        public string currency { get; set; }
        public string Event { get; set; }
        public long timestamp { get; set; }
        public string sessionToken { get; set; }
        public string signature { get; set; }
        public IEnumerable<RefundListEbet> refundList { get; set; }
    }

    public class RefundListEbet
    {
        public string refundSeqNo { get; set; }
        public string username { get; set; }
        public string roundCode { get; set; }
        public decimal refundMoney { get; set; }
    }

    public class AutoBatchRefund_EBet
    {
        public int channelId { get; set; }
        public long lastStartProcessTime { get; set; }
        public decimal refundThreshold { get; set; }
        public string sessionToken { get; set; }
        public string seqNo { get; set; }
        public string currency { get; set; }
        public string Event { get; set; }
        public long timestamp { get; set; }
        public string signature { get; set; }
        public IEnumerable<BatchRefundList> batchRefundList { get; set; }
    }

    public class BatchRefundList
    {
        public string seqNo { get; set; }
        public string username { get; set; }
        public string roundCode { get; set; }
        public decimal refundMoney { get; set; }
    }

    public class CreditRecord_EBet
    {
        public string querySeqNo { get; set; }
        public int type { get; set; }
        public string username { get; set; }
        public string roundCode { get; set; }
        public int status { get; set; }
        public long creditTime { get; set; }
        public decimal moneyBefore { get; set; }
        public decimal moneyAfter { get; set; }
        public decimal money { get; set; }
        public int TicketStatus { get; set; }
    }

    public class SettleData_EBet
    {
        public string seqNo { get; set; }
        public string username { get; set; }
        public string TransId { get; set; }
        public decimal totalBet { get; set; }
        public decimal validBet { get; set; }
        public decimal payout { get; set; }
        public long payoutTime { get; set; }
    }
}
