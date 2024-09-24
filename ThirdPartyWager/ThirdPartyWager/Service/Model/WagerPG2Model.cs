namespace ThirdPartyWager.Service.Model;

public class CurrencyStats_PG2
{
    public Status_PG2 status { get; set; }
    public CurrencyStatsData_PG2 data { get; set; }
}

public class Status_PG2
{
    public int code { get; set; }
    public string message { get; set; }

}

public class CurrencyStatsData_PG2
{
    public int totalWinlose { get; set; }
    public int totalPageAmount { get; set; }
    public IList<CurrencyStatsItem_PG2> transactions { get; set; }

}

public class CurrencyStatsItem_PG2
{
    public string username { get; set; }

    public string gameName { get; set; }
    public string gameRound { get; set; }
    public decimal bet {  get; set; }
    public decimal winlose { get; set; }
    public balance balance { get; set; }
    public string products { get; set; }
    public string categories { get; set; }
    public string timestamp { get; set; }
    public string TpCurrencyId { get; set; }
}

public class balance
{
    public decimal before { get; set; }
    public decimal after { get; set; }
}