using SabaSimulator.API.Models.SabaPlus;
namespace SabaSimulator.API.Models.SabaPlusPlaceBet;

public class SabaPlusPlaceBetResult: GameProviderResponse<PlaceBetResponsePayload>;

public record PlaceBetResponsePayload(string? OperatorCustId, decimal? Balance, PlaceBetResultDataSet[]? BetDataSet);

public class PlaceBetResultDataSet
{
	public string? TicketNo { get; set; }

	public string? RefNo { get; set; }
}
