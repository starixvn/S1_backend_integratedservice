namespace SabaSimulator.API.Models.SabaPlusEndGame;
using SabaSimulator.API.Models.SabaPlus;
public class SabaPlusEndGameRequest : GameProviderRequest<EndGameRequestPayload>;

public record EndGameRequestPayload(string? OperatorCustId, string? GameType, EndGameDataSet[]? BetDataSet)
{
}

public class EndGameDataSet
{
	public string? TicketNo { get; set; }

	public string? GameId { get; set; }

	public string? GameRoundId { get; set; }

	public string? TransactionId { get; set; }

	public decimal? ActualStake { get; set; }

	public decimal? NetStake { get; set; }

	public decimal? Payout { get; set; }

	public decimal? Odds { get; set; }

	public string? GameResult { get; set; }

	public string? SettleDate { get; set; }

	public string? SettleDateWithTz { get; set; }

	public string? ResultId { get; set; }

	public Dictionary<string, string>? OtherBetInfo { get; set; } = null;

	public int? Status { get; set; }
}

