namespace ThirdPartySeamlessWallet.Service.Model;

public class SsessionRequest
{
    public string token { get; set; }
}

public class MockSsessionResponse
{
    public int status { get; set; }
    public string account_id { get; set; }
    public string username { get; set; }
    public string country { get; set; }
    public string token { get; set; }
    public int balance { get; set; }
    public string currency { get; set; }

    public MockSsessionResponse(string token)
    {
        status = 0;
        account_id = "DNova88_127591";
        username = "DNova88_127591";
        country = "";
        token = token;
        balance = 1000;
        currency = "THB";
    }
}

public class BalanceRequest
{
    public string token { get; set; }

    public string account_id { get; set; }
}

public class MockBalanceResponse
{
    public int status { get; set; }
    public string account_id { get; set; }
    public string country { get; set; }
    public string token { get; set; }
    public int balance { get; set; }
    public string currency { get; set; }
    public int bonus_amount { get; set; }

    public MockBalanceResponse(string token)
    {
        status = 0;
        account_id = "DNova88_127591";
        country = "";
        token = token;
        balance = 1000;
        currency = "THB";
        bonus_amount = 0;
    }
}

public class BettingData_DragonGaming
{
}
