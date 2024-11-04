using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    #region ErrorCode

    //1 Indicates FAIL, 0 indicates SUCCESS,
    public enum StatusOfDragonGaming
    {
        [Description("Success,Fix Issue Request.")]
        IssueSuccess = 2,
        [Description("Success,Duplicate Request.")]
        DuplicateSuccess = 1,
        [Description("Success")]
        Success = 0,
    }

    public enum ErrorIdOfDragonGaming
    {
        [Description("The error should be returned in the response on the Bet request.")]
        Insufficient = 100,
        [Description("Player authentication failed due to invalid, not found or expired token.")]
        PlayerInvalid = 200,
        [Description("Player is frozen/offline.")]
        PlayerFrozen = 201,
        [Description("Internal server error.not allow game_type.")]
        GameTypeInvalid = 202,
        [Description("Internal server error.Debit error.")]
        DebitInternalError = 300,
        [Description("Internal server error.Credit error.")]
        CreditInternalError = 400,
        [Description("not exist vaild bet.")]
        NoExistVaildBet = 401,
        [Description("Internal server error.Refund error.")]
        RefundInternalError = 500,
        [Description("Internal server error.")]
        InternalError = 999,
    }

    #endregion
}
