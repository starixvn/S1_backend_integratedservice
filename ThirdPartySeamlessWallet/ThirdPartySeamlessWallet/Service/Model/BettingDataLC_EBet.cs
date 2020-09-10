using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public IList<int?> srcResults { get; set; }
        public IList<int?> results { get; set; }
        public IList<BetListEbet> betList { get; set; }
        public decimal totalBet { get; set; }
        public decimal validBet { get; set; }
        public decimal payout { get; set; }
        public long payoutTime { get; set; }
    }

    public class BetListEbet
    {
        public int betType { get; set; }
        public decimal betMoney { get; set; }
        public string seqNo { get; set; }
        public decimal payout { get; set; }
    }

    public class CancelBetData_EBet
    {
        public int channelId { get; set; }
        public string username { get; set; }
        public string roundCode { get; set; }
        public string seqNo { get; set; }
        public string Event { get; set; }
        public long timestamp { get; set; }
        public string sessionToken { get; set; }
        public string signature { get; set; }
        public IList<ResultListEbet> resultList { get; set; }
    }

    public class ResultListEbet
    {
        public string refundSeqNo { get; set; }
        public string username { get; set; }
        public string roundCode { get; set; }
        public decimal refundMoney { get; set; }
    }

    public class BatchCancelBetData_EBet
    {
        public int channelId { get; set; }
        public int lastStartProcessTime { get; set; }
        public decimal refundThreshold { get; set; }
        public string sessionToken { get; set; }
        public string seqNo { get; set; }
        public string currency { get; set; }
        public string Event { get; set; }
        public long timestamp { get; set; }
        public string signature { get; set; }
        public IList<BatchRefundList> batchRefundList { get; set; }
    }

    public class BatchRefundList
    {
        public string seqNo { get; set; }
        public string username { get; set; }
        public string roundCode { get; set; }
        public decimal refundMoney { get; set; }
    }
}
