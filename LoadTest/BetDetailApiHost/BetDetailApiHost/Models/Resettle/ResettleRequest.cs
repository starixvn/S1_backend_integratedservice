namespace BetDetailApiHost.Models.Resettle;

public record ResettleRequest(string key, ResettleData message);

public record ResettleData
{
    public required string Action { get; set; } = "Resettle"; // Default value for action

    public required string OperationId { get; set; }

    public required ResettlementTransaction[] Txns { get; set; }
}

public record ResettlementTransaction
{
    public required string UserId { get; set; }

    public required string RefId { get; set; }

    public required long TxId { get; set; }

    public required string UpdateTime { get; set; }

    public required string WinlostDate { get; set; }

    public required string Status { get; set; } // half won/half lose/won/lose/draw/void/refund/reject

    public required decimal Payout { get; set; }

    public required decimal CreditAmount { get; set; }

    public required decimal DebitAmount { get; set; }

    public string? SettlementTime { get; set; }

    public string? ExtraStatus { get; set; } //cashout || void ticket || refund match || void cashout

    public ExtraInfo ExtraInfo { get; set; }
}

public record ExtraInfo
{
    public bool IsOnlyWinlostDateChanged { get; set; }
}