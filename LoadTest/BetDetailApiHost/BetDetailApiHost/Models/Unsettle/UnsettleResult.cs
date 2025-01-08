namespace BetDetailApiHost.Models.Unsettle;

public record UnsettleResult
(
    string status,
    string? msg = null
);