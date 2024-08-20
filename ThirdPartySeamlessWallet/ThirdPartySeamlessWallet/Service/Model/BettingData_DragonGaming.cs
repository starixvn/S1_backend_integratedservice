namespace ThirdPartySeamlessWallet.Service.Model;

#region request

public class CommonTokenRequest
{
    public string token { get; set; }
}

public class BalanceRequest : CommonTokenRequest
{
    public string account_id { get; set; }
}

public class CommonDebitCreditRefundRequest : BalanceRequest
{
    public int amount { get; set; }
    public string amount_type { get; set; }
    public string currency { get; set; }
    public int game_id { get; set; }
    public string transaction_id { get; set; }
    public int round_id { get; set; }
    public string game_type { get; set; }
    public string game_name { get; set; }
    public string note { get; set; }
}

public class DebitRequest : CommonDebitCreditRefundRequest
{
    public string bonus_id { get; set; }
}

public class CreditRequest : CommonDebitCreditRefundRequest
{

    public string bonus_id { get; set; }
    public Boolean round_end_state { get; set; }
    public Boolean isfreespins { get; set; }
}

public class RefundRequest : CommonDebitCreditRefundRequest
{
    public string original_transaction_id { get; set; }
}


#endregion

#region Response

public class CommonResponse
{
    public int status { get; set; }
    public string account_id { get; set; }
    public string country { get; set; }
    public string token { get; set; }
    public int balance { get; set; }
    public string currency { get; set; }
}

public class SsessionResponse : CommonResponse
{
    public string username { get; set; }

    public SsessionResponse(string token)
    {
        status = -1;
        account_id = "";
        username = "";
        token = token;
        balance = 0;
        currency = "";
    }

    public SsessionResponse(string tpUserName, string token)
    {
        status = -2;
        account_id = tpUserName;
        username = tpUserName;
        token = token;
        balance = 0;
        currency = "";
    }

    public SsessionResponse(string tpUserName, string reqToken, decimal custBalance, string tpCurrency)
    {
        status = 0;
        account_id = tpUserName;
        username = tpUserName;
        token = reqToken;
        balance = (int)custBalance;
        currency = tpCurrency;
    }
}

public class BalanceResponse : CommonResponse
{
    public int bonus_amount { get; set; }


    public BalanceResponse(string tpUserName, string reqToken, decimal custBalance, string tpCurrency)
    {
        status = 0;
        account_id = tpUserName;
        token = reqToken;
        balance = (int)custBalance;
        currency = tpCurrency;
        bonus_amount = 0;
    }
}

public class DebitCreditRefundResponse : BalanceResponse
{
    public DebitCreditRefundResponse(string tpUserName, string token, decimal balance, string currency) : base(tpUserName, token, balance, currency)
    {
    }

    public string transaction_id { get; set; }
}

#endregion

public class BettingData_DragonGaming
{
}
