namespace BetDetailApiHost.Models.PlaceBetParlay;

public record PlaceBetParlayRequest(
    string key,
    PlaceBetParlayData message
);