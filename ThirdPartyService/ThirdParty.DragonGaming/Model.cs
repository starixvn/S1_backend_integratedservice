namespace ThirdParty.DragonGaming;

#region Login
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

#endregion

#region GetBetDetail

internal class GameRoundHistoryRequest
{
    public string api_key { get; set; }
    public string game_id { get; set; }
    public string player_id { get; set; }
    public string round_id { get; set; }
    public string language { get; set; }
}

internal class GameRoundHistoryResponse
{
    public GameRoundHistoryResult result { get; set; }
}

internal class GameRoundHistoryResult
{
    public string round_info { get; set; }
}

#endregion