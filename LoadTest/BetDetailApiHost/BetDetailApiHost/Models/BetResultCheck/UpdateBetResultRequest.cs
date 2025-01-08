namespace SabaSimulator.API.Models.BetResultCheck;

public class UpdateBetResultRequest
{
	public string? TicketNo { get; set; }

	public BetResultCheckResponse? BetResult { get; set; }
}
