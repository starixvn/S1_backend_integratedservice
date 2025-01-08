namespace BetDetailApiHost.Models.Settle;

public record SettleResult
(
    string status,
    string? msg = null
);