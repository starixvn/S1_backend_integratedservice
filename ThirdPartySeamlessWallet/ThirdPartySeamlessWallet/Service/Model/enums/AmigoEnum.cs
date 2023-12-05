using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    #region ErrorCode

    public enum ErrorCodeOfAmigo
    {
        [Description("Success")]
        Success = 0,
        [Description("The error should be returned in the response on the Bet request.")]
        Insufficient = 1,
        [Description("Player authentication failed due to invalid, not found or expired token.")]
        PlayerInvalid = 4,
        [Description("Player is frozen.")]
        PlayerFrozen = 6,
        [Description("Bet limit has been reached.")]
        BetLimitReached = 50,
        [Description("Internal server error.")]
        InternalError = 100
    }

    #endregion
}
