using SabaSimulator.API.Models.SabaPlus;

namespace SabaSimulator.API.Models.FreeSpin;

public class SabaPlusFreeSpinRequest : GameProviderRequest<FreeSpinRequestPayload>;

public record FreeSpinRequestPayload(string? OperatorCustId, string? WalletSideToken, string? GameType, string? IpAddress, FreeSpinDataSet[] BetDataSet)
{
};

public class FreeSpinDataSet
{
	public string? TicketNo { get; set; }

	public string? GameId { get; set; }

	public string? GameRoundId { get; set; }

	public string? TransactionId { get; set; }

	public decimal? Stake { get; set; }

	public decimal? ActualStake { get; set; }

	public decimal? NetStake { get; set; }

	public decimal? Payout { get; set; }

	public string? SettleDate { get; set; }

	public string? SettleDateWithTz { get; set; }

	public string? BetChoice { get; set; }

	public Dictionary<string, string>? OtherBetInfo { get; set; }
}