namespace BetDetailApiHost.Models.Settle;


public record SettleRequest(string key, SettleData message);

public record SettleData
{
    public string Action { get; set; } = "Settle"; // Default value for action

    public string OperationId { get; set; }

    public SettlementTransaction[] Txns { get; set; }
}

public record SettlementTransaction
{
    public string UserId { get; set; }

    public string RefId { get; set; }

    public long TxId { get; set; }

    public string UpdateTime { get; set; }

    public string WinlostDate { get; set; }

    public string Status { get; set; }

    public decimal Payout { get; set; }

    public decimal CreditAmount { get; set; }

    public decimal DebitAmount { get; set; }

    public string? SettlementTime { get; set; }

    public string? ExtraStatus { get; set; } //cashout || void ticket || refund match || void cashout
}