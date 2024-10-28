using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model;

public class BettingDataLC_BG
{
    public int SQ { get; set; }
    public string random { get; set; } = "";
    public string sn { get; set; } = "";
    public string userId { get; set; } = "";
    public string loginId { get; set; } = "";
    public decimal amount { get; set; } = 0;
    public int moduleId { get; set; } = 0;
    public string tranId { get; set; } = "";
    public string serverTime { get; set; } = null;
    public string gameResult { get; set; } = "";
    public int recalc { get; set; } = 0;
    public string orderId { get; set; } = "";
    public int gameId { get; set; } = 0;
    public decimal orderAmount { get; set; } = 0;
    public string issueId { get; set; } = "";
    public string playId { get; set; } = "";
    public BGOrderFromEnum orderFrom { get; set; } = BGOrderFromEnum.None;
    public string fromIp { get; set; } = "";
    public string orderTime { get; set; } = null;
    public string noComm { get; set; } = "";
    public string betContent { get; set; } = "";
    public decimal calcAmount { get; set; } = 0;
    public decimal validAmount { get; set; } = 0;
    public BGOrderStatusEnum orderStatus { get; set; } = BGOrderStatusEnum.UnSettle;
    public BettransLC_BGTypeEnum FromAPIType { get; set; }
    public BettransType TicketType { get; set; }
    public TicketStatus TicketStatus { get; set; }
}

public class SettleData_BG
{
    public string Account { get; set; }
    /// <summary>
    /// 對應藍位orderStatus:0=注單不存在,1=未結算,2=結算贏,3=結算和,4=結算輸,5=取消(未使用),6=過期(未使用),7=系統取消
    /// </summary>
    public int Status { get; set; }
    public decimal Stake { get; set; }
    public decimal ValidBet { get; set; }
    public decimal WinLostAmount { get; set; }
    public string GameId { get; set; }
    public long TransTime { get; set; }
    public long SettleTime { get; set; }
}

#region Request Model

public class SignParameters_BG
{
    public string? sign { get; set; }
    public string? random { get; set; }
    public string? sn { get; set; }
    public string? loginId { get; set; }
    public string? amount { get; set; }
}

/// <summary>
/// Common Request Model
/// </summary>
public class SeamlessWalletRequest
{
    public string id { get; set; }
    public string method { get; set; }
    public object @params { get; set; }
    public string jsonrpc { get; set; }
}

public class UserBalanceParameters_BG
{
    /// <summary>
    /// 随机字符串，建议使用UUID
    /// </summary>
    public string random { get; set; }
    /// <summary>
    /// 签名摘要sign=md5(random+sn+loginId+secretKey)
    /// </summary>
    public string sign { get; set; }
    /// <summary>
    /// 厅代码
    /// </summary>
    public string sn { get; set; }
    /// <summary>
    /// 用户ID
    /// </summary>
    public string userId { get; set; }
    /// <summary>
    /// 用户登录ID
    /// </summary>
    public string loginId { get; set; }
}

public class OrderTransferParameters_BG
{
    /// <summary>
    /// 随机字符串，建议使用UUID
    /// </summary>
    public string random { get; set; }
    /// <summary>
    /// 签名摘要sign=md5(random+sn+loginId+amount+secretKey)
    /// </summary>
    public string sign { get; set; }
    /// <summary>
    /// 厅代码
    /// </summary>
    public string sn { get; set; }
    /// <summary>
    /// 用户ID
    /// </summary>
    public string userId { get; set; }
    /// <summary>
    /// 用户登录ID
    /// </summary>
    public string loginId { get; set; }
    /// <summary>
    /// 合计金额，支持两位小数(固定为负数，表示扣钱)
    /// </summary>
    public string amount { get; set; }
    /// <summary>
    /// 模块ID，调用方需提供模块基础资料
    /// </summary>
    public int moduleId { get; set; }
    /// <summary>
    /// 业务批次ID
    /// </summary>
    public string tranId { get; set; }
    /// <summary>
    /// 我们(BG)服务器收到的时间
    /// </summary>
    public string serverTime { get; set; }
    /// <summary>
    /// 下注单列表
    /// </summary>
    public IEnumerable<OrderTransferOrders_BG> orders { get; set; }
}

public class OrderTransferOrders_BG
{
    /// <summary>
    /// 订单ID(下注单号)
    /// </summary>
    public long orderId { get; set; }
    /// <summary>
    /// Game ID，标识一个游戏，调用方需提供游戏清单
    /// </summary>
    public int gameId { get; set; }
    /// <summary>
    /// 基础投注金额(牛牛投注翻倍时金额为基础投注额的5倍)
    /// </summary>
    public decimal amount { get; set; }
    /// <summary>
    /// 期数(或者局号)
    /// </summary>
    public string issueId { get; set; }
    /// <summary>
    /// 玩法ID，标识游戏中的玩法，调用方需提供与游戏ID对应的玩法清单
    /// </summary>
    public string playId { get; set; }
    /// <summary>
    /// 注单来源. 1:pc; 2:Android; 3:iOs; 4:pad; 5:h5
    /// </summary>
    public int orderFrom { get; set; }
    /// <summary>
    /// 注单来源IP
    /// </summary>
    public string fromIp { get; set; }
    /// <summary>
    /// 投注时间
    /// </summary>
    public string orderTime { get; set; }
    /// <summary>
    /// 是否免佣.1, 免拥;0,非免拥
    /// </summary>
    public int? noComm { get; set; }
    /// <summary>
    /// 投注內容(需另外開通)
    /// </summary>
    public string betContent { get; set; }
}

public class CalcTransferParameters_BG
{
    /// <summary>
    /// 随机字符串，建议使用UUID
    /// </summary>
    public string random { get; set; }
    /// <summary>
    /// 签名摘要sign=md5(random+sn+loginId+amount+secretKey)
    /// </summary>
    public string sign { get; set; }
    /// <summary>
    /// 厅代码
    /// </summary>
    public string sn { get; set; }
    /// <summary>
    /// 用户ID
    /// </summary>
    public string userId { get; set; }
    /// <summary>
    /// 用户登录ID
    /// </summary>
    public string loginId { get; set; }
    /// <summary>
    /// 合计结算金额，order注单结算(sum(amount))总金额
    /// 负数，表示扣钱;正数,表示加钱
    /// 至多支持2位小数的浮点数float的字符串
    /// </summary>
    public string amount { get; set; }
    /// <summary>
    /// 模块ID，调用方需提供模块基础资料
    /// </summary>
    public int moduleId { get; set; }
    /// <summary>
    /// 业务批次ID
    /// </summary>
    public string tranId { get; set; }
    /// <summary>
    /// 我们(BG)服务器收到的时间
    /// </summary>
    public string serverTime { get; set; }
    /// <summary>
    /// 游戏结果
    /// </summary>
    public string gameResult { get; set; }
    /// <summary>
    /// 是否重新结算. 0,否(默认);1,是(纠正注单错误时)
    /// </summary>
    public string recalc { get; set; }
    /// <summary>
    /// 下注单列表
    /// </summary>
    public IEnumerable<CalcTransferOrders_BG> orders { get; set; }
}

public class CalcTransferOrders_BG
{
    /// <summary>
    /// 订单ID(下注单号)
    /// </summary>
    public long orderId { get; set; }
    /// <summary>
    /// Game ID(游戏ID)
    /// </summary>
    public int gameId { get; set; }
    /// <summary>
    /// 下注金额
    /// </summary>
    public decimal orderAmount { get; set; }
    /// <summary>
    /// 结算金额
    /// </summary>
    public decimal amount { get; set; }
    /// <summary>
    /// 注单状态
    /// </summary>
    public int orderStatus { get; set; }
    /// <summary>
    /// 打码量(有效投注额)
    /// </summary>
    public decimal validAmount { get; set; }
    /// <summary>
    /// 最近更新時間
    /// </summary>
    public string lastUpdateTime { get; set; }
}

public class OrderStatusParameters_BG
{
    /// <summary>
    /// 随机字符串，建议使用UUID
    /// </summary>
    public string random { get; set; }
    /// <summary>
    /// 签名摘要sign=md5(random+sn+secretKey)
    /// </summary>
    public string sign { get; set; }
    /// <summary>
    /// 厅代码
    /// </summary>
    public string sn { get; set; }
    /// <summary>
    /// 模块ID，调用方需提供模块基础资料
    /// </summary>
    public int moduleId { get; set; }
    /// <summary>
    /// 业务批次ID
    /// </summary>
    public string tranId { get; set; }
    /// <summary>
    /// 注单ID列表
    /// </summary>
    public IEnumerable<long> orderIds { get; set; }
    /// <summary>
    /// 注单ID与会员的关系
    /// </summary>
    public IEnumerable<OrderStatusOrderMap_BG> orderMap { get; set; }
}

public class OrderStatusOrderMap_BG
{
    /// <summary>
    /// 注单ID
    /// </summary>
    public long orderId { get; set; }
    /// <summary>
    /// 用户ID[uid]
    /// </summary>
    public long userId { get; set; }
    /// <summary>
    /// 会员登录ID
    /// </summary>
    public string loginId { get; set; }
}

#endregion

#region Response Model

/// <summary>
/// Common Response Model
/// </summary>
public class ResponseModel_BG
{
    public string id { get; set; }
    public object result { get; set; }
    public object error { get; set; }
    public string jsonrpc { get; set; }

    //Ping Response-Success
    public ResponseModel_BG(SeamlessWalletRequest request, long result)
    {
        id = request.id;
        this.result = result;
        jsonrpc = request.jsonrpc;
    }

    //Balance Response-Success
    public ResponseModel_BG(SeamlessWalletRequest request, decimal result)
    {
        id = request.id;
        this.result = result;
        jsonrpc = request.jsonrpc;
    }

    //Bet & Payout(ReSettle) Response-Success
    public ResponseModel_BG(SeamlessWalletRequest request, TransferModel_BG result)
    {
        id = request.id;
        this.result = result;
        jsonrpc = request.jsonrpc;
    }

    //OrderStatus Response-Success
    public ResponseModel_BG(SeamlessWalletRequest request, IEnumerable<OrderStatusModel_BG> result)
    {
        id = request.id;
        this.result = result;
        jsonrpc = request.jsonrpc;
    }

    //Common Response-Error
    public ResponseModel_BG(SeamlessWalletRequest request, BGErrorCode errorCode, string message, string sn = "", string reason = null)
    {
        id = request.id;
        result = "0";
        error = new { code = errorCode, sn = sn, message = message, reason = reason ?? message };
        jsonrpc = request.jsonrpc;
    }
}

public class TransferModel_BG
{
    /// <summary>
    /// 1: 成功; 0: 失败
    /// </summary>
    public string orderResult { get; set; }
    /// <summary>
    /// 厅代码
    /// </summary>
    public string sn { get; set; }
    /// <summary>
    /// 用户ID
    /// </summary>
    public long userId { get; set; }
    /// <summary>
    /// 业务批次ID
    /// </summary>
    public string transId { get; set; }
    /// <summary>
    /// 会员最新的余额
    /// </summary>
    public decimal availableAmount { get; set; }

    public TransferModel_BG(string sn, string userId, string transId, decimal availableAmount)
    {
        orderResult = "1";
        this.sn = sn;
        this.userId = long.Parse(userId);
        this.transId = transId;
        this.availableAmount = availableAmount;
    }
}

public class OrderStatusModel_BG
{
    /// <summary>
    /// 注单ID
    /// </summary>
    public long orderId { get; set; }
    /// <summary>
    /// 注单状态
    /// </summary>
    public int status { get; set; }

    public OrderStatusModel_BG(long orderId, int status)
    {
        this.orderId = orderId;
        this.status = status;
    }
}

#endregion

public class BGConfig : TpSiteConfig
{
    public Dictionary<string, CurrencyDataBG> CurrencyData { get; set; }
}
public class CurrencyDataBG
{
    public string Sn { get; set; }
    public string AgentId { get; set; }
    public string AgentLoginId { get; set; }
    public string ApiSecretKey { get; set; }
    public string AgentPassword { get; set; }
}