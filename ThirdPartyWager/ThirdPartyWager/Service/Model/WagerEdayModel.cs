using System.Text.Json.Serialization;

namespace ThirdPartyWager.Service.Model
{
    public class WagerEdayModel
    {
        [JsonPropertyName("resultCode")]
        public int ResultCode { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public EdayResultData Data { get; set; }
    }

    public class EdayResultData
    {
        [JsonPropertyName("items")]
        public List<WagerEdayBillData> Items { get; set; }

        [JsonPropertyName("pageIndex")]
        public int PageIndex { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class WagerEdayBillData
    {
        [JsonPropertyName("billId")]
        public string BillId { get; set; }

        [JsonPropertyName("player")]
        public string Player { get; set; }

        [JsonPropertyName("gameCode")]
        public string GameCode { get; set; }

        [JsonPropertyName("roundCode")]
        public string RoundCode { get; set; }

        [JsonPropertyName("billStatus")]
        public int BillStatus { get; set; }

        [JsonPropertyName("createdTime")]
        public string CreatedTime { get; set; }

        [JsonPropertyName("endTime")]
        public string EndTime { get; set; }

        [JsonPropertyName("settlementTime")]
        public string SettlementTime { get; set; }

        [JsonPropertyName("totalBet")]
        public decimal TotalBet { get; set; }

        [JsonPropertyName("totalWin")]
        public decimal TotalWin { get; set; }

        [JsonPropertyName("totalNet")]
        public decimal TotalNet { get; set; }

        [JsonPropertyName("itemCount")]
        public int ItemCount { get; set; }

        [JsonPropertyName("items")]
        public List<EdayResultItem> Items { get; set; }
        public int WagerType { get; set; }
    }

    public class EdayResultItem
    {
        [JsonPropertyName("referenceId")]
        public int ReferenceId { get; set; }

        [JsonPropertyName("betTypeCode")]
        public string BetTypeCode { get; set; }

        [JsonPropertyName("playNumber")]
        public string PlayNumber { get; set; }

        [JsonPropertyName("betAmount")]
        public decimal BetAmount { get; set; }

        [JsonPropertyName("payoutRate")]
        public decimal PayoutRate { get; set; }

        [JsonPropertyName("winAmount")]
        public decimal WinAmount { get; set; }
    }
    
    public class EdayBillDetailModel
    {
        [JsonPropertyName("resultCode")]
        public int ResultCode { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public WagerEdayBillData Data { get; set; }
    }
}