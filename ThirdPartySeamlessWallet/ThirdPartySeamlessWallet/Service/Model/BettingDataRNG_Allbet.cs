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
    
    public class AllBetCancelTransferRequest
    {
        [JsonProperty("player")]
        public string Player { get; set; }
        
        [JsonProperty("tranId")]
        public long TranId { get; set; }
        
        [JsonProperty("originalTranId")]
        public long OriginalTranId { get; set; }
        
        [JsonProperty("originalDetails", NullValueHandling = NullValueHandling.Ignore)]
        public List<AllBetTransferDetails> OriginalDetails { get; set; }
        
        [JsonProperty("reason")]
        public string Reason { get; set; }
        
        [JsonProperty("isRetry")]
        public bool IsRetry { get; set; }
    }

    public class AllBetTransferReq
    {
        public AllbetTransferType Type { get; set; }
    }

    public class AllBetTransferRequest<T>
    {
        [JsonProperty("tranId")]
        public long TranId { get; set; }
        
        [JsonProperty("player")]
        public string Player { get; set; }
        
        [JsonProperty("amount")]
        public decimal Amount { get; set; }
        
        [JsonProperty("currency")]
        public string Currency { get; set; }
        
        [JsonProperty("type")]
        public AllbetTransferType Type { get; set; }
        
        [JsonProperty("isRetry")]
        public bool IsRetry { get; set; }
        
        [JsonProperty("details", NullValueHandling = NullValueHandling.Ignore)]
        public List<T> Details { get; set; }
    }

    public class AllBetTransferDetails
    {
        [JsonProperty("betNum")]
        public long BetNum { get; set; }
        
        [JsonProperty("gameRoundId")]
        public long GameRoundId { get; set; }
        
        [JsonProperty("status")]
        public int Status { get; set; }
        
        [JsonProperty("betAmount")]
        public decimal BetAmount { get; set; }
        
        [JsonProperty("deposit")]
        public decimal Deposit { get; set; }
        
        [JsonProperty("gameType")]
        public int GameType { get; set; }
        
        [JsonProperty("betType")]
        public int BetType { get; set; }
        
        [JsonProperty("commission")]
        public int Commission { get; set; }
        
        [JsonProperty("gameResult", NullValueHandling = NullValueHandling.Ignore)]
        public string GameResult { get; set; }
        
        [JsonProperty("gameResult2", NullValueHandling = NullValueHandling.Ignore)]
        public string GameResult2 { get; set; }
        
        [JsonProperty("winOrLossAmount", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? WinOrLossAmount { get; set; }
        
        [JsonProperty("validAmount", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? ValidAmount { get; set; }
        
        [JsonProperty("exchangeRate", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? ExchangeRate { get; set; }
        
        [JsonProperty("betTime")]
        public string BetTime { get; set; }

        [JsonProperty("tableName")]
        public string TableName { get; set; }
        
        [JsonProperty("betMethod")]
        public int BetMethod { get; set; }
        
        [JsonProperty("appType")]
        public int AppType { get; set; }
        
        [JsonProperty("gameRoundStartTime")]
        public string GameRoundStartTime { get; set; }
        
        [JsonProperty("gameRoundEndTime", NullValueHandling = NullValueHandling.Ignore)]
        public string GameRoundEndTime { get; set; }
        
        [JsonProperty("ip")]
        public string Ip { get; set; }
    }
    
    public class AllbetTransferEventDetail
    {
        [JsonProperty("eventType")]
        public int EventType { get; set; }
        
        [JsonProperty("eventCode")]
        public string EventCode { get; set; }
        
        [JsonProperty("eventRecordNum")]
        public long EventRecordNum { get; set; }
        
        [JsonProperty("amount")]
        public decimal Amount { get; set; }
        
        [JsonProperty("exchangeRate")]
        public decimal ExchangeRate { get; set; }

        [JsonProperty("settleTime")]
        public string SettleTime { get; set; }
    }
    
    public class AllBetTransferResponse
    {
        [JsonProperty("resultCode")]
        public int ResultCode { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("balance")]
        public decimal Balance { get; set; }
        
        [JsonProperty("version")]
        public int Version { get; set; }
        
        public AllBetTransferResponse(AllbetResultCode returnCode, decimal amount = 0)
        {
            ResultCode = (int)returnCode;
            Message = returnCode.GetEnumDescription();
            Balance = amount;
            Version = 1;
        }

        public AllBetTransferResponse(AllbetResultCode returnCode, string msg)
        {
            ResultCode = (int)returnCode;
            Message = msg;
        }
        public AllBetTransferResponse(AllbetException exception)
        {
            ResultCode = exception.resultCode;
            Message = exception.message;
        }
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
