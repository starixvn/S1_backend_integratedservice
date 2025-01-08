using SabaSimulator.API.Models.SabaPlus;

namespace SabaSimulator.API.Models.SabaPlusPlaceBet;
public class SabaPlusPlaceBetRequest : GameProviderRequest<SabaPlusPlaceBetRequestPayload>;

public record SabaPlusPlaceBetRequestPayload(
	string? OperatorCustId,
	string? WalletSideToken,
	string? GameType,
	string? IpAddress,
	SabaPlusPlaceBetDataSet[] BetDataSet)
{
	public string? GameProviderCode => WalletSideToken?.Split('@')[0];
};

public class SabaPlusPlaceBetDataSet
{
	public string? TicketNo { get; set; }

	public string? GameId { get; set; }

	public string? GameRoundId { get; set; }

	public string? TransactionId { get; set; }

	public decimal? Stake { get; set; }

	public decimal? ActualStake { get; set; }

	public decimal? Odds { get; set; }

	public string? BetChoice { get; set; }

	public string? TableCode { get; set; }

	public Dictionary<string, string>? OtherBetInfo { get; set; }
}