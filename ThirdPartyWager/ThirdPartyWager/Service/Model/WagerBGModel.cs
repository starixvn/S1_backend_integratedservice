namespace ThirdPartyWager.Service.Model;

public class WagerBG_ApiResponseModel<T>
{
    public string id { get; set; }
    public T result { get; set; }
    public object error { get; set; }
    public string jsonrpc { get; set; }
}

public class WagerBG_OrderQueryResultResponseModel
{
    public int total { get; set; }
    public object stats { get; set; }
    public IEnumerable<WagerBG_OrderQueryItemsModel> items { get; set; }
    public int pageIndex { get; set; }
    public int pageSize { get; set; }
    public string etag { get; set; }
    public string page { get; set; }
}


public class WagerBG_OrderQueryItemsModel : WagerBGModel
{
}

public class WagerBG_OrderSumResultResponseModel
{
    public int total { get; set; }
    public WagerBG_OrderSumStatsModel stats { get; set; }
    public IEnumerable<object> items { get; set; }
    public int pageIndex { get; set; }
    public int pageSize { get; set; }
    public string etag { get; set; }
    public string page { get; set; }
}

public class WagerBG_OrderSumStatsModel
{
    public decimal paymentamount { get; set; }
    public decimal orderamount { get; set; }
    public int usercount { get; set; }
    public decimal validorderamount { get; set; }
    public decimal validbetamount { get; set; }
    public int ordercount { get; set; }
}


public class WagerBGModel
{
    public string orderId { get; set; } = "";
    public string tranId { get; set; } = "";
    public string sn { get; set; } = "";
    public string uid { get; set; } = "";
    public string userId { get; set; } = "";
    public string loginId { get; set; } = "";
    public int moduleId { get; set; }
    public string moduleName { get; set; } = "";
    public int gameId { get; set; }
    public string gameName { get; set; } = "";
    public string gameNameEn { get; set; } = "";
    public int orderStatus { get; set; }
    public decimal bAmount { get; set; }
    public decimal aAmount { get; set; }
    public int orderFrom { get; set; }
    public string orderTime { get; set; } = "";
    public string lastUpdateTime { get; set; } = "";
    public string fromIp { get; set; } = "";
    public string tableId { get; set; } = "";
    public string issueId { get; set; } = "";
    public string playId { get; set; } = "";
    public string playName { get; set; } = "";
    public string playNameEn { get; set; } = "";
    public decimal validBet { get; set; }
    public decimal payment { get; set; }
    public int? noComm { get; set; }
    public string betContent { get; set; }
}

public class WagerBGTotalModel
{
    public int TotalCount { get; set; }
    public decimal Stake { get; set; }
    public decimal ValidBet { get; set; }
    public decimal Payment { get; set; }

    public decimal WinLostAmount { get; set; }
}
