using SabaSimulator.API.Models.SabaPlus;

namespace SabaSimulator.API.Models.FreeSpin;

public class SabaPlusFreeSpinResponse : GameProviderResponse<FreeSpinPayload>;

public record FreeSpinPayload(string OperatorCustId, decimal Balance, FreeSpinPayloadResultDataSet[] BetDataSet);

public class FreeSpinPayloadResultDataSet
{
	public string TicketNo { get; set; } = default!;

	public string RefNo { get; set; } = default!;
}