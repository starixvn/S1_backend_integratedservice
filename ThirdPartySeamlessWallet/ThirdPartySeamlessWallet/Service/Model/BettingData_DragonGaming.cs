using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.enums;
using static ThirdPartySeamlessWallet.Controllers.APController;

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
    public string transaction_id { get; set; }
    public int round_id { get; set; }
    public string game_type { get; set; }
    public string game_name { get; set; }
    public string note { get; set; }
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
    public string original_transaction_id { get; set; }
}


#endregion

#region Response

public class CommonResponse_DragonGaming
{
    public int status { get; set; }
    public string account_id { get; set; }
    public string country { get; set; }
    public string token { get; set; }
    public long balance { get; set; }
    public string currency { get; set; }
    public string message { get; set; }

    public CommonResponse_DragonGaming(ErrorCodeOfDragonGaming errorCode, string token)
    {
        status = errorCode > 0 ? (int)ErrorCodeOfDragonGaming.Success : (int)errorCode;
        this.token = token;
        this.message = errorCode.GetEnumDescription();
    }
}

public class SessionResponse_DragonGaming : CommonResponse_DragonGaming
{
    public string username { get; set; }

    public SessionResponse_DragonGaming(ErrorCodeOfDragonGaming errorCode, string token)
        : base(errorCode, token)
    {
        account_id = "";
        username = "";
        balance = 0;
        currency = "";
    }

    public SessionResponse_DragonGaming(ErrorCodeOfDragonGaming errorCode, string token, string tpUserName)
        : base(errorCode, token)
    {
        account_id = tpUserName;
        username = tpUserName;
        balance = 0;
        currency = "";
    }

    public SessionResponse_DragonGaming(ErrorCodeOfDragonGaming errorCode, string token, string tpUserName, decimal balance, string currency)
        : base(errorCode, token)
    {
        account_id = tpUserName;
        username = tpUserName;
        this.balance = (int)balance;
        this.currency = currency;
    }
}

public class BalanceResponse_DragonGaming : CommonResponse_DragonGaming
{
    public int bonus_amount { get; set; }

    public BalanceResponse_DragonGaming(ErrorCodeOfDragonGaming errorCode, string token) : base(errorCode, token)
    {
        bonus_amount = 0;
    }

    public BalanceResponse_DragonGaming(ErrorCodeOfDragonGaming errorCode, string token, string tpUserName, decimal balance, string currency)
        : base(errorCode, token)
    {
        account_id = tpUserName;
        this.balance = (long)(balance * 100);
        this.currency = currency;
        bonus_amount = 0;
    }
}

public class DebitCreditRefundResponse_DragonGaming : BalanceResponse_DragonGaming
{

    public DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming errorCode, string token) : base(errorCode, token)
    {
    }

    public DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming errorCode, string token, string tpUserName, string currency, string transaction_id)
        : base(errorCode, token)
    {
        account_id = tpUserName;
        this.currency = currency;
        this.transaction_id = transaction_id;
    }

    public DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming errorCode, string token, string tpUserName, decimal balance, string currency, string transaction_id)
        : base(errorCode, token, tpUserName, balance, currency)
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

        transaction_id = request.transaction_id;
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

        transaction_id = request.transaction_id;
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

        transaction_id = request.transaction_id;
        round_id = request.round_id.ToString();
        game_type = request.game_type;
        game_name = request.game_name;
        note = request.note;

        TicketType = ticketType;
    }
}
