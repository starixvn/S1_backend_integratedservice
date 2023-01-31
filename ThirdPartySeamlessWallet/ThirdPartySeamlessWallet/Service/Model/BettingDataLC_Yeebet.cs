using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model;

public class BettingDataLC_Yeebet
{

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
    public int username { get; set; }
    /// <summary>
    /// 讯息号，每一次通知的讯息号都不一样，但须注意网络原因的重送机制会造成重复收到，商户端可利用notifyid判断防止重复扣款100032155222301
    /// </summary>
    public decimal notifyid { get; set; }
    /// <summary>
    /// 加密秘钥
    /// </summary>
    public decimal sign { get; set; }
}

public class BalanceRequest_Yeebet: RequestModel_Yeebet
{
}

public class DeductRequest_Yeebet : RequestModel_Yeebet
{
    /// <summary>
    /// 金额(负数)
    /// </summary>
    public decimal amount { get; set; }
    /// <summary>
    /// 讯息编号
    /// </summary>
    public int type { get; set; }
    /// <summary>
    /// 请求的订单号（注单，打赏记录编号）
    /// </summary>
    public string serialnumber { get; set; }
}

public class DepositRequest_Yeebet : RequestModel_Yeebet
{
    /// <summary>
    /// 金额(正数)
    /// </summary>
    public decimal amount { get; set; }
    /// <summary>
    /// 讯息编号
    /// </summary>
    public int type { get; set; }
    /// <summary>
    /// 请求的订单号（注单，打赏记录编号）
    /// </summary>
    public string serialnumber { get; set; }
}

public class RollbackRequest_Yeebet : RequestModel_Yeebet
{
    /// <summary>
    /// 金额(正数)
    /// </summary>
    public decimal amount { get; set; }
    /// <summary>
    /// 讯息编号
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
    /// 描述
    /// </summary>
    public string desc { get; set; }
    /// <summary>
    /// 结果信息(0成功，失败<0)
    /// </summary>
    public int result { get; set; }
    /// <summary>
    /// 用户余额
    /// </summary>
    public decimal balance { get; set; }
}

public class BalanceResponse_Yeebet : ResponseModel_Yeebet
{
    public BalanceResponse_Yeebet(string desc = "", int result = 0, decimal balance = 0)
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
    public DeductResponse_Yeebet(string serialnumber, string orderno, string desc = "", int result = 0, decimal balance = 0)
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
    public DepositResponse_Yeebet(string orderno, string desc = "", int result = 0, decimal balance = 0)
    {
        this.orderno = orderno;
        this.desc = desc;
        this.result = result;
        this.balance = balance;
    }
}

public class RollbackResponse_Yeebet : ResponseModel_Yeebet
{
    public RollbackResponse_Yeebet(string desc = "", int result = 0, decimal balance = 0)
    {
        this.desc = desc;
        this.result = result;
        this.balance = balance;
    }
}

#endregion
