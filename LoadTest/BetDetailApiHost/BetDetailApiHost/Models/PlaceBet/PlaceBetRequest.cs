namespace BetDetailApiHost.Models.PlaceBet;

public record PlaceBetRequest(
   string key,
   PlaceBetData message
   );

public record PlaceBetData
{
	public string Action { get; set; } = "PlaceBet"; 
	public string OperationId { get; set; }
	public string UserId { get; set; }
	public int Currency { get; set; }
	public int MatchId { get; set; }
	public int HomeId { get; set; }
	public int AwayId { get; set; }
	public string HomeName { get; set; }
	public string AwayName { get; set; }
	public string KickOffTime { get; set; }
	public string BetTime { get; set; }
	public decimal BetAmount { get; set; }
	public decimal ActualAmount { get; set; }
	public int SportType { get; set; }
	public string SportTypeName { get; set; }
	public int BetType { get; set; }
	public string BetTypeName { get; set; }
	public short OddsType { get; set; }
	public int OddsId { get; set; }
	public decimal Odds { get; set; }
	public string? BetChoice { get; set; }
	public string? BetChoice_en { get; set; }
	public string UpdateTime { get; set; }
	public int LeagueId { get; set; }
	public string LeagueName { get; set; }
	public string LeagueName_en { get; set; }
	public string SportTypeName_en { get; set; }
	public string HomeName_en { get; set; }
	public string AwayName_en { get; set; }
	public string BetTypeName_en { get; set; }
	public string IP { get; set; }
	public bool IsLive { get; set; }
	public string RefId { get; set; }
	public string? TsId { get; set; }
	public string? Point { get; set; }
	public string? Point2 { get; set; }
	public string? BetTeam { get; set; }
	public int? HomeScore { get; set; }
	public int? AwayScore { get; set; }
	public bool BaStatus { get; set; }
	public string? Excluding { get; set; }
	public string BetFrom { get; set; }
	public decimal CreditAmount { get; set; }
	public decimal DebitAmount { get; set; }
	public string? OddsInfo { get; set; }
	public string MatchDateTime { get; set; }
	public string? BetRemark { get; set; }
	public string? VendorTransId { get; set; }
	public decimal? MmrPercentage { get; set; }
	public List<Voucher>? Voucher { get; set; }

}
public record Voucher
{
	public int? Type { get; set; }
	public decimal? Quota { get; set; }
}