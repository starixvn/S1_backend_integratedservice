namespace ThirdPartyWager.Service.Model;

public class WagerBG_OrderQueryResponseModel
{
    public string id { get; set; }
    public WagerBG_OrderQueryResultResponseModel result { get; set; }
    public object error { get; set; }
    public string jsonrpc { get; set; }
    public IEnumerable<WagerBGModel> array { get; set; }
}

public class WagerBG_OrderQueryResultResponseModel
{
    public int total { get; set; }
    public object stats { get; set; }
    public IEnumerable<WagerBGModel> items { get; set; }
    public int pageIndex { get; set; }
    public int pageSize { get; set; }
    public string etag { get; set; }
    public string page { get; set; }
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

