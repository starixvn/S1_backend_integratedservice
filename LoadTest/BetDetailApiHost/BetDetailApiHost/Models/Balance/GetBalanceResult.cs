namespace BetDetailApiHost.Models.Balance;

public record GetBalanceResult
(
    string status,
    string? userId,
    decimal? balance,
    string? balanceTs,
    string? msg = null
);