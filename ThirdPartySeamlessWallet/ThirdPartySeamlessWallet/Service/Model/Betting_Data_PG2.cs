using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model;

[Table("BettingDataRNG_PG2")]
public class BettingDataRNG_PG2
{
    public int SQ { get; set; }
    public string username { get; set; }
    public string agent { get; set; }
    public string type { get; set; }
    public decimal amount { get; set; }
    public string product { get; set; }
    public string category { get; set; }
    public string gameName { get; set; }
    public string roundId { get; set; }
    public string refId { get; set; }
    public decimal bet { get; set; }
    
    public DateTime timestamp { get; set; }
    public bool isFeatureBuy { get; set; }
    
    public BettransRng_PG2TypeEnum FromAPIType { get; set; }
    public BettransType TicketType { get; set; }
    public TicketStatus TicketStatus { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}
#region request
public class CommonRequest_PG2
{
    public string UserName { get; set; }
    public string Agent { get; set; }
}

public class BetRequest_PG2 : CommonRequest_PG2
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    [JsonProperty("product")]
    public string Product { get; set; }

    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("gameName")]
    public string GameName { get; set; }

    [JsonProperty("roundId")]
    public string RoundId { get; set; }

    [JsonProperty("refId")]
    public string RefId { get; set; }

    [JsonProperty("timestamp")]
    public string Timestamp { get; set; }

    [JsonProperty("isFeatureBuy")]
    public bool IsFeatureBuy { get; set; }
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
    public Status_PG2 status { get; set; }
    public BalanceData_PG2 data { get; set; }
}

public class BetResponse_PG2
{
    public Status_PG2 status { get; set; }
    public BetData_PG2 data { get; set; }
}

public class Status_PG2
{
    public PG2ErrorCode code { get; set; }
    public string message { get; set; }
}

public class BalanceData_PG2
{
    public decimal balance { get; set; }
}

public class BetData_PG2
{
    public string username { get; set; }

    public walletmodel wallet { get; set; }

    public balancemodel balance { get; set; }
    public string refId { get; set; }
}
public class walletmodel
{
    public decimal balance { get; set; }

    public string lastUpdate { get; set; }
}
public class balancemodel
{
    public decimal before { get; set; }

    public decimal after { get; set; }
}
#endregion
