namespace BetDetailApiHost.Models.ConfirmBet;

public record ConfirmBetResult(
        string status,
        decimal? balance = null,
        string? msg = null
        );