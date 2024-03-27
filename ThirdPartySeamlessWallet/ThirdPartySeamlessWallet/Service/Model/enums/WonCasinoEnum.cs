using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums;

public enum WonCasinoTypeEnum
{
    Bet = 1,
    Cancel = 7,
    Settle = 9,
    ReSettle = 10,
    RollBack = 12,
}

public enum WonCasinoStateEnum
{
   Void = -2,
   Cancel = -1,
   Rollback = -3,
   Bet = 0,
   Settle = 1,
}

public enum WonCasinoErrorCodeEnum
{
    [Description("OK")]
    Success = 0,
    [Description("The system is error, please contact")]
    SystemError = -1001,
    [Description("Insufficient deduce amount")]
    InsufficientBalance = -1030,
    [Description("User does not exist")] 
    PlayerInvalid = -1012,
    [Description("User is prohibited")] 
    PlayerFrozen = -1014,
    [Description("Sign error")] 
    SignError = -1007,
    [Description("Deduct Type is in an illegal state, Please Check.")]
    TypeError = -1000,
}


