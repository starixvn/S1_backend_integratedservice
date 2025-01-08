namespace BetDetailApiHost.Models.ConfirmbetParlay;

public record ConfirmBetParlayData
{
	public string Action { get; set; } = "ConfirmBetParlay";
	public string OperationId { get; set; }
	public string UserId { get; set; }
	public string UpdateTime { get; set; }
	public decimal CreditAmount { get; set; }
	public decimal DebitAmount { get; set; }
	public List<TicketInfo> Txns { get; set; }
	public List<BetParlayTicketDetail> TicketDetail { get; set; }
	public string TransactionTime { get; set; }
}

public record TicketInfo
{
	public string RefId { get; set; }
	public long TxId { get; set; }
	public string LicenseeTxId { get; set; }
	public decimal ActualAmount { get; set; }
	public bool IsOddsChanged { get; set; }
	public decimal CreditAmount { get; set; }
	public decimal DebitAmount { get; set; }
	public string WinlostDate { get; set; }
	public decimal? Odds { get; set; }
}

public record BetParlayTicketDetail
{
	public int MatchId { get; set; }
	public int SportType { get; set; }
	public int BetType { get; set; }
	public int OddsId { get; set; }
	public decimal Odds { get; set; }
	public short OddsType { get; set; }
	public int LeagueId { get; set; }
	public bool IsLive { get; set; }
	public bool IsOddsChanged { get; set; }
}