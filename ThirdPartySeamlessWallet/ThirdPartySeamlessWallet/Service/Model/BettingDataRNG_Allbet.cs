using Newtonsoft.Json;
using System.Collections.Generic;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataLC_Allbet
    {
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("client")]
        public string Client { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("details")]
        public List<Detail> Details { get; set; }

        [JsonProperty("originalTranId")]
        public long OriginalTranId { get; set; }

        [JsonProperty("tranId")]
        public long TranId { get; set; }

        [JsonProperty("transferType")]
        public int TransferType { get; set; }
    }

    public class AllBetTransferReq
    {
        public AllbetTransferType type { get; set; }
    }

    public class AllBetTransferRequest<T>
    {
        [JsonProperty("tranId")]
        public long tranId { get; set; }
        
        [JsonProperty("player")]
        public string player { get; set; }
        
        [JsonProperty("amount")]
        public decimal amount { get; set; }
        
        [JsonProperty("currency")]
        public string currency { get; set; }
        
        [JsonProperty("type")]
        public AllbetTransferType type { get; set; }
        
        [JsonProperty("isRetry")]
        public bool isRetry { get; set; }
        
        [JsonProperty("details", NullValueHandling = NullValueHandling.Ignore)]
        public List<T> details { get; set; }
    }

    public class AllBetTransferDetails
    {
        [JsonProperty("betNum")]
        public long betNum { get; set; }
        
        [JsonProperty("gameRoundId")]
        public long gameRoundId { get; set; }
        
        [JsonProperty("status")]
        public int status { get; set; }
        
        [JsonProperty("betAmount")]
        public decimal betAmount { get; set; }
        
        [JsonProperty("deposit")]
        public decimal deposit { get; set; }
        
        [JsonProperty("gameType")]
        public int gameType { get; set; }
        
        [JsonProperty("betType")]
        public int betType { get; set; }
        
        [JsonProperty("commission")]
        public int commission { get; set; }
        
        [JsonProperty("gameResult")]
        public decimal gameResult { get; set; }
        
        [JsonProperty("gameResult2")]
        public string gameResult2 { get; set; }
        
        [JsonProperty("winOrLossAmount", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? winOrLossAmount { get; set; }
        
        [JsonProperty("validAmount", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? validAmount { get; set; }
        
        [JsonProperty("betTime")]
        public string betTime { get; set; }

        [JsonProperty("tableName")]
        public string tableName { get; set; }
        
        [JsonProperty("betMethod")]
        public int betMethod { get; set; }
        
        [JsonProperty("appType")]
        public int appType { get; set; }
        
        [JsonProperty("gameRoundStartTime")]
        public string gameRoundStartTime { get; set; }
        
        [JsonProperty("gameRoundEndTime", NullValueHandling = NullValueHandling.Ignore)]
        public string gameRoundEndTime { get; set; }
        
        [JsonProperty("ip")]
        public string ip { get; set; }
    }
    
    public class AllbetTransferEventDetail
    {
        [JsonProperty("eventType")]
        public int eventType { get; set; }
        
        [JsonProperty("eventCode")]
        public string eventCode { get; set; }
        
        [JsonProperty("eventRecordNum")]
        public long eventRecordNum { get; set; }
        
        [JsonProperty("amount")]
        public decimal amount { get; set; }
        
        [JsonProperty("exchangeRate")]
        public decimal exchangeRate { get; set; }

        [JsonProperty("settleTime")]
        public string settleTime { get; set; }
    }
    
    public class AllBetTransferResponse
    {
        public int resultCode { get; set; }
        public string message { get; set; }
        public decimal balance { get; set; }
        public int version { get; set; }
        
        public AllBetTransferResponse(AllbetResultCode returnCode, decimal amount = 0)
        {
            resultCode = (int)returnCode;
            message = returnCode.GetEnumDescription();
            balance = amount;
            version = 1;
        }

        public AllBetTransferResponse(AllbetResultCode returnCode, string msg)
        {
            resultCode = (int)returnCode;
            message = msg;
        }
        public AllBetTransferResponse(AllbetException exception)
        {
            resultCode = exception.resultCode;
            message = exception.message;
        }
        // public AllBetTransferResponse(ExecuteResult result)
        // {
        //     resultCode = (int)result.ReturnCode;
        //     message = result.Message;
        // }
    }


    public class Detail
    {
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("betNum", NullValueHandling = NullValueHandling.Ignore)]
        public long? BetNum { get; set; }

        [JsonProperty("eventRecordNum", NullValueHandling = NullValueHandling.Ignore)]
        public long? EventRecordNum { get; set; }

        [JsonProperty("eventCode", NullValueHandling = NullValueHandling.Ignore)]
        public string EventCode { get; set; }

        [JsonProperty("eventType", NullValueHandling = NullValueHandling.Ignore)]
        public int? EventType { get; set; }
    }

    public class UnsettleBettingDataLC_Allbet
    {
        public long BetNum { get; set; }
        public string Client { get; set; }
    }
}
