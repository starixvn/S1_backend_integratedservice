namespace BetDetailApiHost.Models.PlaceBetParlay;

public record PlaceBetParlayResult(string status, List<TicketInfoMapping>? txns, string? msg);

public record TicketInfoMapping(string? refId, string? licenseeTxId);