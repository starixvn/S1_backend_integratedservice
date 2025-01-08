using SabaSimulator.API.Models.SabaPlus;

namespace SabaSimulator.API.Models.SabaPlusPlaceBetAndSettle;

public class SabaPlusPlaceBetAndSettleResponse : GameProviderResponse<PlaceBetAndSettlePayload>;

public record PlaceBetAndSettlePayload(string OperatorCustId, decimal Balance, PlaceBetAndSettleResultDataSet[] BetDataSet);

public class PlaceBetAndSettleResultDataSet
{
	public string TicketNo { get; set; }

	public string RefNo { get; set; }
}