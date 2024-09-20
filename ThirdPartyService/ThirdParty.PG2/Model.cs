using Newtonsoft.Json;
using ThirdPartyCore.Model;
namespace ThirdParty.PG2;


public class CommonResponse
{
    public Status status { get; set; }
    public Data? data { get; set; }
}

public class Status
{
    public int code { get; set; }
    public string message { get; set; }
}

public class Data
{
    public string url { get; set; }
}

public class GameLaunchRequest
{
    [JsonProperty("username")]
    public string UserName { get; set; }

    [JsonProperty("agent")]
    public string Agent { get; set; }

    [JsonProperty("partner")]
    public string Partner { get; set; }

    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("gameId")]
    public string GameId { get; set; }

    [JsonProperty("backLink")]
    public string BackLink { get; set; }
}

internal class GameLaunchResponse
{
    public Status Status { get; set; }
    public Data Data { get; set; }
}

internal class PG2Config : TpSiteConfig
{
    public Dictionary<string, CurrencyDataPG2> CurrencyData { get; set; }
}

public class CurrencyDataPG2
{
    public string ApiKey { get; set; }
    public string Agent { get; set; }
}