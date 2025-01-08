namespace BetDetailApiHost.Models.CancelBet;

public record CancelBetResult
(
    string status,
    decimal? balance = null,
    string? msg = null
);