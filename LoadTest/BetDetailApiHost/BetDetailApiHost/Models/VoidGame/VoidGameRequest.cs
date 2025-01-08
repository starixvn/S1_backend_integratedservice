using SabaSimulator.API.Models.SabaPlus;

namespace SabaSimulator.API.Models.VoidGame;

public class VoidGameRequest : GameProviderRequest<VoidGameRequestPayload>;

public record VoidGameRequestPayload(string OperatorCustId, string GameType, VoidGameRequestDataSet[] BetDataSet)
{
	public string Action { get; set; } = "VoidGame - Simulator";
}

public class VoidGameRequestDataSet
{
	public int Status { get; set; }

	public string TicketNo { get; set; }

	public string GameId { get; set; }

	public string GameRoundId { get; set; }

	public string? TransactionId { get; set; }
}