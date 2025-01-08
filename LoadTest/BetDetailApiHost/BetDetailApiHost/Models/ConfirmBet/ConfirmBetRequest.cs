namespace BetDetailApiHost.Models.ConfirmBet;

public record ConfirmBetRequest(string key, ConfirmBetData message);

public record ConfirmBetData
{
    public string Action { get; set; } = "ConfirmBet"; // Default value for action
    public required string OperationId { get; set; }
    public required string UserId { get; set; }
    public required string UpdateTime { get; set; }

    public required string TransactionTime { get; set; }

    public required ConfirmTicketInfo[] Txns { get; set; }
}

public record ConfirmTicketInfo
{
    public required string RefId { get; set; }

    public required long TxId { get; set; }

    public required string LicenseeTxId { get; set; }

    public required decimal Odds { get; set; }

    public required short OddsType { get; set; }

    public required decimal ActualAmount { get; set; }

    public required bool IsOddsChanged { get; set; }

    public required decimal CreditAmount { get; set; }

    public required decimal DebitAmount { get; set; }

    public required string WinlostDate { get; set; }

    public decimal MmrPercentage { get; set; }

    public bool IsMmrPercentageChanged { get; set; }
}