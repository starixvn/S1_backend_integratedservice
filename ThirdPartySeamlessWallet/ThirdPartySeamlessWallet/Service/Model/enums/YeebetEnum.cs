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
