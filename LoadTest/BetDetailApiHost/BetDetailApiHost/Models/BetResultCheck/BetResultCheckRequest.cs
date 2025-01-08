namespace SabaSimulator.API.Models.BetResultCheck;

public class BetResultCheckRequest
{
	public string? TimeStamp { get; set; }
	public string? Seq { get; set; }
	public PayloadModel? Payload { get; set; }
}

public class PayloadModel
{
	public string? GameType { get; set; }
	public string? OperatorCustId { get; set; }
	public string? TicketNo { get; set; }
	public string? TransDate { get; set; }
	public AgentInfoModel? AgentInfo { get; set; }
}

public class AgentInfoModel
{
	public string? AgentId { get; set; }
	public string? AgentKey { get; set; }
	public string? AgentCode { get; set; }
	public string? ExtraInfo { get; set; }
}