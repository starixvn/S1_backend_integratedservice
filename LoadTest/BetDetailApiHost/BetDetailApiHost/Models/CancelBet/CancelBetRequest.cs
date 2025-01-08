namespace BetDetailApiHost.Models.CancelBet;

public record CancelBetRequest(
       string key,
       CancelBetData message
       );

public record CancelBetData
{
    public string Action { get; set; } = "CancelBet";

    public required string UserId { get; set; }

    public required string OperationId { get; set; }

    public required string UpdateTime { get; set; }

    public required string ErrorMessage { get; set; }

    public required CancelBetTransaction[] Txns { get; set; }
}

public record CancelBetTransaction
{
    public required string RefId { get; set; }

    public required decimal CreditAmount { get; set; }

    public required decimal DebitAmount { get; set; }
}