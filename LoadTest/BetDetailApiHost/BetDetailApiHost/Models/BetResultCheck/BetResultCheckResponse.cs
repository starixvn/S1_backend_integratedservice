namespace SabaSimulator.API.Models.BetResultCheck;

public class BetResultCheckResponse
{
	public string? Seq { get; set; }
	public int? ErrorCode { get; set; }
	public string? ErrorMessage { get; set; }
	public string? TimeStamp { get; set; } 
	public BetResultPayloadModel? Payload { get; set; }
}

public class BetResultPayloadModel
{
	public string? TicketNo { get; set; }
	public string? OperatorCustId { get; set; }
	public string? GameRoundId { get; set; }
	public string? TransactionId { get; set; }
	public short? Status { get; set; } 
	public decimal? ActualStake { get; set; }
	public decimal? NetStake { get; set; }
	public decimal? Payout { get; set; }
	public string?  SettleDate { get; set; }  
	public string? GameResult { get; set; } 
	public decimal? Odds { get; set; } 
	public Dictionary<string, string>? OtherBetInfo { get; set; }
}

