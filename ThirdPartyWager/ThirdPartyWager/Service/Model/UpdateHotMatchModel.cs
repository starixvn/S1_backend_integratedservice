namespace ThirdPartyWager.Service.Model;

public class UpdateHotMatchModel
{
}

public class HotMatchInfo
{
    public int MatchId { get; set; }
    public int LeagueId { get; set; }
    public string LeagueName { get; set; } = string.Empty;
    public DateTime KickOffTime { get; set; }
    public bool IsLive { get; set; }
    public string HomeTeamName { get; set; }
    public string AwayTeamName { get; set; }
    public string HomeIconUrl { get; set; }
    public string AwayIconUrl { get; set; }
    public byte Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public string LastUpdateUser { get; set; }
    public decimal HomeOdds { get; set; }
    public decimal AwayOdds { get; set; }
    public string Handicap { get; set; }
    public int? Sort { get; set; }
    public DateTime CreateTime { get; set; }
}

public class HotMatchRequest
{
    public string Matches { get; set; }
}
