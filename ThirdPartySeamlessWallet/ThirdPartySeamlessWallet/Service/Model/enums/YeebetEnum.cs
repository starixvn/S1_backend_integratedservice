using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums;

public enum YeebetErrorCode
{
    [Description("Success")]
    Success = 0,
    [Description("Insufficient balance")]
    InsufficientBalance = -1030,
    [Description("System error")]
    SystemError = -1001
}

public enum BettransLC_YeebetTypeEnum
{
    FromDeposit = 1,
    FromWithdraw = 2,
    FromRollback = 3
}

public enum typeEnum
{
    /// <summary>
    /// 下注
    /// </summary>
    Betting = 1,
    /// <summary>
    /// 撤單/取消下注
    /// </summary>
    CancelOrderOrBetting = 7,
    /// <summary>
    /// 派彩
    /// </summary>
    Awards = 9,
    /// <summary>
    /// 重新派彩
    /// </summary>
    ReEvaluation = 10,
}

public enum stateEnum
{
    /// <summary>
    /// 失敗
    /// </summary>
    Fail = -3,
    /// <summary>
    /// 撤銷
    /// </summary>
    Cancel = -2,
    /// <summary>
    /// 廢棄
    /// </summary>
    Abandoned = -1,
    /// <summary>
    /// 投注
    /// </summary>
    Bet = 0,
    /// <summary>
    /// 結算/更正(重新結算，repayout)
    /// </summary>
    Settle = 1
}
