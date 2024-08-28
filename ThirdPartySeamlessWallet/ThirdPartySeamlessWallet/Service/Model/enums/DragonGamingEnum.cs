using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    #region ErrorCode

    //ErrorCode >=0 Success. ErrorCode < 0 Error.
    public enum ErrorCodeOfDragonGaming
    {
        [Description("Success,Duplicate Request.")]
        DuplicateSuccess = 1,
        [Description("Success")]
        Success = 0,
        [Description("The error should be returned in the response on the Bet request.")]
        Insufficient = -1,
        [Description("Player authentication failed due to invalid, not found or expired token.")]
        PlayerInvalid = -4,
        [Description("Player is frozen/offline.")]
        PlayerFrozen = -6,
        [Description("Internal server error.Debit error.")]
        DebitInternalError = -100,
        [Description("Internal server error.Credit error.")]
        CreditInternalError = -200,
        [Description("not exist vaild bet.")]
        NoExistVaildBet = -201,
        [Description("Internal server error.Refund error.")]
        RefundInternalError = -300,
        [Description("Internal server error.")]
        InternalError = -999
    }

    #endregion
}
