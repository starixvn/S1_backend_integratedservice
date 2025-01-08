namespace BetDetailApiHost.Models.PlaceBet;

public record PlaceBetResult(
	string status,
	string? refId = null,
	string? licenseeTxId = null,
	string? msg = null
	);