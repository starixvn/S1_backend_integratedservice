using Newtonsoft.Json;

namespace ThirdPartyWager.Service.Model;

public class HotMatchResponseModel
{
    [JsonProperty("err")]
    public int ErrorCode { get; set; }

    [JsonProperty("errmsg")]
    public string ErrorMessage { get; set; }

    [JsonProperty("data")]
    public IEnumerable<Match> Matches { get; set; }
}

public class Match
{
    [JsonProperty("eventId")]
    public int MatchId { get; set; }

    [JsonProperty("leagueId")]
    public int LeagueId { get; init; }

    [JsonProperty("leagueName")]
    public string LeagueName { get; init; } = default!;

    [JsonProperty("kickOffTime")]
    public DateTime KickOffTime { get; init; }

    [JsonProperty("teamInfo")]
    public TeamInfo TeamInfo { get; init; } = default!;

    [JsonProperty("markets")]
    public IEnumerable<MatchMarket> Markets { get; set; }
}

public class TeamInfo
{
    [JsonProperty("homeId")]
    public int HomeId { get; init; }

    [JsonProperty("homeName")]
    public string HomeName { get; init; } = default!;

    [JsonProperty("homeIconUrl")]
    public string HomeIconUrl { get; init; } = default!;

    [JsonProperty("awayId")]
    public int AwayId { get; init; }

    [JsonProperty("awayName")]
    public string AwayName { get; init; } = default!;

    [JsonProperty("awayIconUrl")]
    public string AwayIconUrl { get; init; } = default!;
}

public class MatchMarket
{
    [JsonProperty("eventId")]
    public int EventID { get; set; }

    [JsonProperty("betType")]
    public int BetType { get; set; }

    [JsonProperty("marketID")]
    public int MarketID { get; set; }

    [JsonProperty("marketCode")]
    public double MarketCode { get; set; }

    [JsonProperty("selections")]
    public List<MatchSelection> Selections { get; set; }

    [JsonProperty("resourceid")]
    public object Resourceid { get; set; }
}

public class MatchSelection
{
    [JsonProperty("price")]
    public string Price { get; set; }

    [JsonProperty("oddsPrice")]
    public OddsPrice OddsPrice { get; set; }

    [JsonProperty("key")]
    public string Key { get; set; }

    [JsonProperty("point")]
    public string Point { get; set; }

    [JsonProperty("point2")]
    public object Point2 { get; set; }
}

public class OddsPrice
{
    [JsonProperty("ParlayPrice")]
    public decimal ParlayPrice { get; set; }

    [JsonProperty("MalayPrice")]
    public decimal MalayPrice { get; set; }

    [JsonProperty("DecimalPrice")]
    public decimal DecimalPrice { get; set; }

    [JsonProperty("HongKongPrice")]
    public decimal HongKongPrice { get; set; }

    [JsonProperty("IndoPrice")]
    public decimal IndoPrice { get; set; }

    [JsonProperty("AmericanPrice")]
    public decimal AmericanPrice { get; set; }
}

