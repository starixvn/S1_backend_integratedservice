namespace BetDetailApiHost.Models.Unsettle;

public record UnsettleRequest(string key, UnsettleData message);

public record UnsettleData
{
    public required string Action { get; set; } = "Unsettle"; // Default value for action

    public required string OperationId { get; set; }

    public required UnsettleTransaction[] Txns { get; set; }
}

public record UnsettleTransaction
{
    public required string UserId { get; set; }

    public required string RefId { get; set; }

    public required long TxId { get; set; }

    public required string UpdateTime { get; set; }

    public required decimal CreditAmount { get; set; }

    public required decimal DebitAmount { get; set; }

    public string? ExtraStatus { get; set; } //cashout || void ticket || refund match || void cashout
}