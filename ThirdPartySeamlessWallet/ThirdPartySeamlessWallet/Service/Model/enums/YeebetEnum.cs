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

/// <summary>
/// 對應tp文件2.8.1
/// </summary>
public enum typeEnum
{
    /// <summary>
    /// Default
    /// </summary>
    None = 0,
    /// <summary>
    /// 下注
    /// </summary>
    Betting = 1,
    /// <summary>
    /// 取消下注
    /// </summary>
    CancelOrder = 7,
    /// <summary>
    /// 派彩
    /// </summary>
    Awards = 9,
    /// <summary>
    /// 重新派彩
    /// </summary>
    ReEvaluation = 10,
    /// <summary>
    /// 回滾(經確認，暫無使用)
    /// </summary>
    Rollback = 12
}

public enum BettransLC_YeebetTypeEnum
{
    FromDeposit = 1,
    FromWithdraw = 2,
    FromRollback = 3
}
