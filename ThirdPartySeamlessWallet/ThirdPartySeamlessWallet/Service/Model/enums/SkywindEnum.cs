namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum SkywindErrorCode
    {
        NoErrors = 0,
        DuplicateTransaction = 1,
        MerchantInternalError = -1,
        PlayerNotFound = -2,
        GameTokenExpired = -3,
        PlayerIsSuspended = -301,
        BetLimitWasExceeded = -302,
        InsufficientBalance = -4,
        InsufficientFreeBetsBalance = -5,
        InvalidFreeBet = -6,
        TransactionNotFound = -7,
    }
}
