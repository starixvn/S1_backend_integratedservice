using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model;

public class BettingDataLC_Yeebet
{
    public int SQ { get; set; }
    public string appid { get; set; } = "";
    public string username { get; set; } = "";
    public decimal amount { get; set; } = 0;
    public string notifyid { get; set; } = "";
    public int type { get; set; } = 0;
    public string serialnumber { get; set; } = "";
    public string errmsg { get; set; } = "";
    public string bets { get; set; } = "";
    public int IsRollback { get; set; } = 0;
    public BettransLC_YeebetTypeEnum FromAPIType { get; set; }
    public BettransType TicketType { get; set; }
    public TicketStatus TicketStatus { get; set; }
}

public class SettleData_Yeebet
{
    public string Account { get; set; }
    /// <summary>
    /// 對應藍位state:-2=撤銷,-1=廢棄，0=投注，1=结算
    /// </summary>
    public int Status { get; set; }
    public decimal Stake { get; set; }
    public decimal ValidBet { get; set; }
    public decimal WinLostAmount { get; set; }
    public long TransTime { get; set; }
    public long SettleTime { get; set; }
    public string GameId { get; set; }
}

public class YeebetConfig : TpSiteConfig
{
    public Dictionary<string, CurrencyDataYeebet> CurrencyData { get; set; }
}

public class CurrencyDataYeebet
{
    public string ApiAppId { get; set; }
    public string ApiSecretKey { get; set; }
    public string SeamlessAppId { get; set; }
    public string SeamlessSecretKey { get; set; }
}

#region Request Model

/// <summary>
/// Common Request Model
/// </summary>
public class RequestModel_Yeebet
{
    /// <summary>
    /// 分配应用ID
    /// </summary>
    public string appid { get; set; }
    /// <summary>
    /// 用户名（唯一识别）
    /// </summary>
    public string username { get; set; }
    /// <summary>
    /// 讯息号，每一次通知的讯息号都不一样，但须注意网络原因的重送机制会造成重复收到，商户端可利用notifyid判断防止重复扣款100032155222301
    /// </summary>
    public string notifyid { get; set; }
    /// <summary>
    /// 加密秘钥
    /// </summary>
    public string sign { get; set; }
}

public class BalanceRequest_Yeebet : RequestModel_Yeebet
{
}

public class DeductRequest_Yeebet : RequestModel_Yeebet
{
    /// <summary>
    /// 金额(负数)
    /// </summary>
    public decimal amount { get; set; }
    /// <summary>
    /// 讯息编号，1=投注,7=撤單(取消投注),9=頒獎(派彩),10=重新評獎(重新派彩),12=回滾
    /// </summary>
    public int type { get; set; }
    /// <summary>
    /// 请求的订单号（注单，打赏记录编号）
    /// </summary>
    public string serialnumber { get; set; }

    /// <summary>
    /// 明細資訊
    /// </summary>
    public string bets { get; set; }
}

public class DepositRequest_Yeebet : RequestModel_Yeebet
{
    /// <summary>
    /// 金额(正数)
    /// </summary>
    public decimal amount { get; set; }
    /// <summary>
    /// 讯息编号，1=投注,7=撤單(取消投注),9=頒獎(派彩),10=重新評獎(重新派彩),12=回滾
    /// </summary>
    public int type { get; set; }
    /// <summary>
    /// 请求的订单号（注单，打赏记录编号）
    /// </summary>
    public string serialnumber { get; set; }
    /// <summary>
    /// 明細資訊
    /// </summary>
    public string bets { get; set; }
}

public class RollbackRequest_Yeebet : RequestModel_Yeebet
{
    /// <summary>
    /// 金额(正数)
    /// </summary>
    public decimal amount { get; set; }
    /// <summary>
    /// 讯息编号，1=投注,7=撤單(取消投注),9=頒獎(派彩),10=重新評獎(重新派彩),12=回滾
    /// </summary>
    public int type { get; set; }
    /// <summary>
    /// 请求的订单号（注单，打赏记录编号）
    /// </summary>
    public string serialnumber { get; set; }
    /// <summary>
    /// 错误消息（URLEncoder）
    /// </summary>
    public string errmsg { get; set; }
}

#endregion


#region Response Model

/// <summary>
/// Common Response Model
/// </summary>
public class ResponseModel_Yeebet
{
    /// <summary>
    /// 结果信息(0成功，失败<0)
    /// </summary>
    public YeebetErrorCode result { get; set; }
    /// <summary>
    /// 描述
    /// </summary>
    public string desc { get; set; }
    /// <summary>
    /// 用户余额
    /// </summary>
    public decimal balance { get; set; }
}

public class BalanceResponse_Yeebet : ResponseModel_Yeebet
{
    public BalanceResponse_Yeebet(YeebetErrorCode result, string desc = "", decimal balance = 0)
    {
        this.desc = desc;
        this.result = result;
        this.balance = balance;
    }
}

public class DeductResponse_Yeebet : ResponseModel_Yeebet
{
    /// <summary>
    /// 商户扣款记录号(回传商户的记录编号，方便后期核账)
    /// </summary>
    public string serialnumber { get; set; }
    /// <summary>
    /// 商户商户记录单号
    /// </summary>
    public string orderno { get; set; }
    public DeductResponse_Yeebet(YeebetErrorCode result, string serialnumber, string orderno, string desc = "", decimal balance = 0)
    {
        this.serialnumber = serialnumber;
        this.orderno = orderno;
        this.desc = desc;
        this.result = result;
        this.balance = balance;
    }
}

public class DepositResponse_Yeebet : ResponseModel_Yeebet
{
    /// <summary>
    /// 商户商户记录单号
    /// </summary>
    public string orderno { get; set; }
    public DepositResponse_Yeebet(YeebetErrorCode result, string orderno, string desc = "", decimal balance = 0)
    {
        this.orderno = orderno;
        this.desc = desc;
        this.result = result;
        this.balance = balance;
    }
}

public class RollbackResponse_Yeebet : ResponseModel_Yeebet
{
    public RollbackResponse_Yeebet(YeebetErrorCode result, string desc = "", decimal balance = 0)
    {
        this.desc = desc;
        this.result = result;
        this.balance = balance;
    }
}

#endregion
