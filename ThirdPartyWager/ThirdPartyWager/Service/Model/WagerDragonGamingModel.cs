namespace ThirdPartyWager.Service.Model;

#region GameHistoryResponse

public class GameHistoryResponse
{
    public int? error { get; set; }

    public ErrorDetail error_detail { get; set; }

    public GameHistoryResult result { get; set; }

    public bool HasError => error.HasValue && error.Value != 0;
}

public class ErrorDetail
{
    public int id { get; set; }
    public string code { get; set; }
    public string message { get; set; }
}

public class GameHistoryResult
{
    public GameHistory game_history { get; set; }
}

public class GameHistory
{
    public List<string> headers { get; set; }
    public List<List<object>> data { get; set; }
}

public class GameHistoryData
{
    public string PlayerID { get; set; }
    public string GameID { get; set; }
    public string GameName { get; set; }
    public string GameCategory { get; set; }
    public string Platform { get; set; }
    public string RoundID { get; set; }
    public decimal BetAmount { get; set; }
    public decimal WinAmount { get; set; }
    public string AmountType { get; set; }
    public string PlayType { get; set; }
    public string DateTime { get; set; }
    public string BetStatus { get; set; }
}

#endregion

public class WagerDragonGamingModel
{
    public string PlayerID { get; set; }
    public string GameID { get; set; }
    public string GameName { get; set; }
    public string GameCategory { get; set; }
    public string Platform { get; set; }
    public string RoundId { get; set; }
    public decimal BetAmount { get; set; }
    public decimal WinAmount { get; set; }
    public string AmountType { get; set; }
    public string PlayType { get; set; }
    public string SettledTime { get; set; }
    public string BetStatus { get; set; }

    public WagerDragonGamingModel(GameHistoryData data)
    {
        PlayerID = data.PlayerID ?? "";
        GameID = data.GameID ?? "";
        GameName = data.GameName ?? "";
        GameCategory = data.GameCategory ?? "";
        Platform = data.Platform ?? "";
        RoundId = data.RoundID ?? "";
        BetAmount = data.BetAmount;
        WinAmount = data.WinAmount;
        AmountType = data.AmountType ?? "";
        PlayType = data.PlayType ?? "";
        SettledTime = data.DateTime ?? "";
        BetStatus = data.BetStatus ?? "";
    }
}
