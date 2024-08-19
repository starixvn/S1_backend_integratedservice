namespace ThirdParty.DragonGaming;

internal class GameLaunchRequest
{
    public string api_key { get; set; }
    public string session_id { get; set; }
    public string privoder { get; set; }
    public string game_type { get; set; }
    public string game_id { get; set; }
    public string platform { get; set; }
    public string language { get; set; }
    public string amount_type { get; set; }
    public string lobby_url { get; set; }
    public string deposit_url { get; set; }
    public GameLaunchContext context { get; set; }
}

internal class GameLaunchContext
{
    public string id { get; set; }

    public string username { get; set; }

    public string country { get; set; }

    public string currency { get; set; }
}

internal class GameLaunchResponse
{
    public GameLaunchResult result { get; set; }
}

internal class GameLaunchResult
{
    public string launch_url { get; set; }
}