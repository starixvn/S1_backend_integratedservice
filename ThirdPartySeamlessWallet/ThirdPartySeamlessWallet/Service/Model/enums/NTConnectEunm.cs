using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{ 
    #region ErrorCode
    
    public enum NTConnectErrorCode
    {
        [Description("Success")] 
        Success = 0,
        [Description("The error should be returned in the response on the Bet request.")]
        InsufficientBalance = 1,
        [Description("The Bet Order is Settle or Cancel.")]
        BetOrderStatusError = 2,
        [Description("The Bet Order already Cancel.")]
        OrderAlreadyCancel = 3,
        [Description("Player authentication failed due to invalid, not found or expired token.")]
        PlayerInvalid = 4,
        [Description("The bet is still ongoing and cannot be canceled.")]
        BetOnGoing = 5,
        [Description("Player is frozen.")] 
        PlayerFrozen = 6,
        [Description("Request status is NullOrEmpty")]
        StatusError = 7,
        [Description("Can't found bet_id.")]
        CantFoundBetId = 50,
        [Description("Internal server error.")]
        InternalError = 999,
        [Description("Exception Error")]
        Exception = 9999
    }
    
    #endregion
}