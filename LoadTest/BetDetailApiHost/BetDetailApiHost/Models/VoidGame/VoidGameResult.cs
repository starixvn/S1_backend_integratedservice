using SabaSimulator.API.Models.SabaPlus;

namespace SabaSimulator.API.Models.VoidGame;

public class VoidGameResult : GameProviderResponse<VoidGameResponsePayload>;

public record VoidGameResponsePayload
{
	public string OperatorCustId { get; set; }

	public decimal Balance { get; set; }
}