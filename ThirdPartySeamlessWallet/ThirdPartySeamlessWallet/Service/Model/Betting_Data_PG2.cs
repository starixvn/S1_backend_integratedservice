using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model;

#region request
public class CommonRequest_PG2
{
    public string UserName { get; set; }
    public string Agent { get; set; }
}

public class BetRequest_PG2 : CommonRequest_PG2
{
    public string UserName { get; set; }
}
public class PayoutRequest_PG2
{
    public string UserName { get; set; }
}
public class SettlementRequest_PG2
{
    public string UserName { get; set; }
}

public class CancelRequest_PG2
{
    public string UserName { get; set; }
}
#endregion

#region response
public class Response_PG2
{
    public Status_PG2 Status { get; set; }
    public Data_PG2 Data { get; set; }

    public Response_PG2(int code, string message)
    {
        Status.Code = code;
        Status.Message = message;
    }
    public Response_PG2(int code, string message, decimal balance)
    {
        Status.Code = code;
        Status.Message = message;
        Data.Balance = balance;
    }
}

public class Status_PG2
{
    public int Code { get; set; }
    public string Message { get; set; }
}

public class Data_PG2
{
    public decimal Balance { get; set; }
}
#endregion
