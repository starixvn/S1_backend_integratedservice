using SabaSimulator.API.Models.SabaPlus;

namespace SabaSimulator.API.Models.Balance;

public class SabaPlusGetBalanceRequest : GameProviderRequest<SabaPlusGetBalanceData>;

public record SabaPlusGetBalanceData
{
	public required string OperatorCustId { get; set; }
	public required string WalletSideToken { get; set; }
	public required string GameType { get; set; }
}