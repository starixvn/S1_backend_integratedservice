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
    public decimal betAmount { get; set; }
    
    public DateTime timestamp { get; set; }
    public bool isFeatureBuy { get; set; }
    public bool isFreeSpin { get; set; }

    public bool isOnlyPayout { get; set; }
    public bool isEndRound { get; set; }
    public string parentRoundId { get; set; }


    public BettransRng_PG2TypeEnum FromAPIType { get; set; }
    public BettransType TicketType { get; set; }
    public TicketStatus TicketStatus { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

public class SettleDataRNG_PG2 : SettleData
{
    public string Categories { get; set; }
    public decimal Bet { get; set; }
    public decimal WinLost { get; set; }
    public decimal Turnover { get; set; }
}
#region request
public class CommonRequest_PG2
{
    [JsonProperty("username")]
    public string UserName { get; set; }
    [JsonProperty("agent")]
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
public class PayoutRequest_PG2 : CommonRequest_PG2
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

    [JsonProperty("isFreespin")]
    public bool IsFreespin { get; set; } 

    [JsonProperty("isOnlyPayout")]
    public bool IsOnlyPayout { get; set; } 
}
public class SettlementRequest_PG2 : CommonRequest_PG2
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    [JsonProperty("product")]
    public string Product { get; set; }

    [JsonProperty("gameName")]
    public string GameName { get; set; }
    [JsonProperty("category")]
    public string Category {  get; set; }

    [JsonProperty("roundId")]
    public string RoundId { get; set; }

    [JsonProperty("refId")]
    public string RefId { get; set; }

    [JsonProperty("timestamp")]
    public string Timestamp { get; set; }

    [JsonProperty("isFeatureBuy")]
    public bool IsFeatureBuy { get; set; }

    [JsonProperty("betAmount")]
    public decimal BetAmount { get; set; }

    [JsonProperty("isEndRound")]
    public bool IsEndRound { get; set; }

    [JsonProperty("parentRoundId")]
    public string ParentRoundId { get; set; }

    [JsonProperty("isOnlyPayout")]
    public bool IsOnlyPayout { get; set; }
}

public class CancelRequest_PG2 : CommonRequest_PG2
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
}

#endregion

#region response
public class BalanceResponse_PG2
{
    public Status_PG2 status { get; set; }
    public BalanceData_PG2 data { get; set; }
}

public class BetResponse_PG2
{
    public Status_PG2 status { get; set; }
    public BetData_PG2 data { get; set; }
}

public class PayoutResponse_PG2
{
    public Status_PG2 status { get; set; }
    public BetData_PG2 data { get; set; }
}

public class CancelResponse_PG2
{
    public Status_PG2 status { get; set; }
    public BetData_PG2 data { get; set; }
}

public class SettleResponse_PG2
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

    public Walletmodel wallet { get; set; }

    public Balancemodel balance { get; set; }
    public string refId { get; set; }
}
public class Walletmodel
{
    public decimal balance { get; set; }

    public string lastUpdate { get; set; }
}
public class Balancemodel
{
    public decimal before { get; set; }

    public decimal after { get; set; }
}
#endregion
