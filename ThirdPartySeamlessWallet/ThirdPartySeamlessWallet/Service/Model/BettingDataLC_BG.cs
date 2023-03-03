using Microsoft.AspNetCore.Razor.Language;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model;

public class BettingDataLC_BG
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

public class SettleData_BG
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
}

#region Request Model

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

public class BalanceParameters_BG
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

    //Common Response-Error
    public ResponseModel_BG(SeamlessWalletRequest request, BGErrorCode errorCode, string message, string sn = "", string reason = null)
    {
        id = request.id;
        result = "0";
        error = new { code = errorCode, sn = sn, message = message, reason = reason ?? message };
        jsonrpc = request.jsonrpc;
    }
}

#endregion
