namespace BetDetailApiHost.Models.Balance;

public record GetBalanceRequest(string Key, GetBalanceData Message);

public record GetBalanceData
{
    public string Action { get; set; } = "GetBalance";

    public required string UserId { get; set; }
}