using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model;

#region Request

public class SessionRequest_DragonGaming
{
    public string token { get; set; }
}

public class BalanceRequest_DragonGaming : SessionRequest_DragonGaming
{
    public string account_id { get; set; }
}

public class CommonDebitCreditRefundRequest_DragonGaming : BalanceRequest_DragonGaming
{
    public int amount { get; set; }
    public string amount_type { get; set; }
    public string currency { get; set; }
    public int game_id { get; set; }
    public object transaction_id { get; set; }
    public long round_id { get; set; }
    public string game_type { get; set; }
    public string game_name { get; set; }
    public string note { get; set; }

    public string GetTransactionIdAsString()
    {
        return transaction_id?.ToString();
    }
}

public class DebitRequest_DragonGaming : CommonDebitCreditRefundRequest_DragonGaming
{
    public string bonus_id { get; set; }
}

public class CreditRequest_DragonGaming : CommonDebitCreditRefundRequest_DragonGaming
{

    public string bonus_id { get; set; }
    public Boolean round_end_state { get; set; }
    public Boolean isfreespins { get; set; }
}

public class RefundRequest_DragonGaming : CommonDebitCreditRefundRequest_DragonGaming
{
    public object original_transaction_id { get; set; }

    public string GetOriginalTransactionIdAsString()
    {
        return original_transaction_id?.ToString();
    }
}


#endregion

#region Response

public class CommonResponse_DragonGaming
{
    public int status { get; set; }

}

public class CommonSuccessResponse_DragonGaming : CommonResponse_DragonGaming
{
    public string account_id { get; set; }
    public string country { get; set; }
    public string token { get; set; }
    public long balance { get; set; }
    public string currency { get; set; }
    public string message { get; set; }

    public CommonSuccessResponse_DragonGaming(string token)
    {
        status = (int)StatusOfDragonGaming.Success;
        this.token = token;
        this.message = StatusOfDragonGaming.Success.GetEnumDescription();
    }
}

public class CommonErrorResponse_DragonGaming : CommonResponse_DragonGaming
{
    public int error_id { get; set; }
    public string error_code { get; set; }
    public string error_message { get; set; }

    public CommonErrorResponse_DragonGaming(ErrorIdOfDragonGaming errorId)
    {
        status = (int)StatusOfDragonGaming.Fail;
        this.error_id = (int)errorId;
        this.error_code = ((int)errorId).ToString();
        this.error_message = errorId.GetEnumDescription();
    }
}

public class SessionResponse_DragonGaming : CommonSuccessResponse_DragonGaming
{
    public string username { get; set; }

    /// <summary>
    /// success session response
    /// </summary>
    /// <param name="token"></param>
    /// <param name="tpUserName"></param>
    /// <param name="balance"></param>
    /// <param name="currency"></param>
    public SessionResponse_DragonGaming(string token, string tpUserName, decimal balance, string currency)
        : base(token)
    {
        account_id = tpUserName;
        username = tpUserName;
        this.balance = (long)(balance * 100);
        this.currency = currency;
    }
}

public class BalanceResponse_DragonGaming : CommonSuccessResponse_DragonGaming
{
    public int bonus_amount { get; set; }

    /// <summary>
    /// success balance response
    /// </summary>
    /// <param name="token"></param>
    /// <param name="tpUserName"></param>
    /// <param name="balance"></param>
    /// <param name="currency"></param>
    public BalanceResponse_DragonGaming(string token, string tpUserName, decimal balance, string currency)
        : base(token)
    {
        account_id = tpUserName;
        this.balance = (long)(balance * 100);
        this.currency = currency;
        bonus_amount = 0;
    }
}

public class DebitCreditRefundResponse_DragonGaming : BalanceResponse_DragonGaming
{
    /// <summary>
    /// success debit/credit/refund response
    /// </summary>
    /// <param name="token"></param>
    /// <param name="tpUserName"></param>
    /// <param name="balance"></param>
    /// <param name="currency"></param>
    /// <param name="transaction_id"></param>
    public DebitCreditRefundResponse_DragonGaming(string token, string tpUserName, decimal balance, string currency, string transaction_id)
        : base(token, tpUserName, balance, currency)
    {
        this.transaction_id = transaction_id;
    }

    public string transaction_id { get; set; }
}

#endregion

public class BettingData_DragonGaming
{
    public string account_id { get; set; } = "";
    public decimal amount { get; set; }
    public string amount_type { get; set; } = "";
    public string currency { get; set; } = "";
    public int game_id { get; set; }
    public string transaction_id { get; set; } = "";
    public string round_id { get; set; } = "";
    public string game_type { get; set; } = "";
    public string game_name { get; set; } = "";
    public string note { get; set; } = "";
    public string bonus_id { get; set; } = "";
    public Boolean round_end_state { get; set; }
    public Boolean isfreespins { get; set; }
    public int TicketType { get; set; }

    public BettingData_DragonGaming()
    {

    }

    public BettingData_DragonGaming(DebitRequest_DragonGaming request, int ticketType)
    {
        account_id = request.account_id;
        amount = request.amount / 100m * -1;
        amount_type = request.amount_type;
        currency = request.currency;
        game_id = request.game_id;

        transaction_id = request.GetTransactionIdAsString();
        round_id = request.round_id.ToString();
        game_type = request.game_type;
        game_name = request.game_name;
        note = request.note;

        bonus_id = request.bonus_id ?? "";
        TicketType = ticketType;
    }

    public BettingData_DragonGaming(CreditRequest_DragonGaming request, int ticketType)
    {
        account_id = request.account_id;
        amount = request.amount / 100m;
        amount_type = request.amount_type;
        currency = request.currency;
        game_id = request.game_id;

        transaction_id = request.GetTransactionIdAsString();
        round_id = request.round_id.ToString();
        game_type = request.game_type;
        game_name = request.game_name;
        note = request.note;

        bonus_id = request.bonus_id ?? "";
        round_end_state = request.round_end_state;
        isfreespins = request.isfreespins;

        TicketType = ticketType;
    }

    public BettingData_DragonGaming(RefundRequest_DragonGaming request, int ticketType)
    {
        account_id = request.account_id;
        amount = request.amount / 100m;
        amount_type = request.amount_type;
        currency = request.currency;
        game_id = request.game_id;

        transaction_id = request.GetTransactionIdAsString();
        round_id = request.round_id.ToString();
        game_type = request.game_type;
        game_name = request.game_name;
        note = request.note;

        TicketType = ticketType;
    }
}
