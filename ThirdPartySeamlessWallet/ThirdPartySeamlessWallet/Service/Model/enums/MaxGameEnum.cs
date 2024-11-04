using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    #region ErrorCode

    public enum ErrorCodeOfMaxGame
    {
        [Description("Success")]
        Success = 0,
        [Description("Unspecified Error")]
        OpSideUnspecifiedError = 1000,
        [Description("The player token is invalid")]
        OpSideTokenInvalid = 1001,
        [Description("The player token expired")]
        OpSideTokenExpired = 1002,
        [Description("The authentication credentials for the API are incorrect")]
        OpSideAuthenticationCredentialsIncorrect = 1003,
        [Description("The Vendor Id is invalid")]
        OpSideVendorIdInvalid = 1004,
        [Description("The sign data is invalid")]
        OpSideSignInvalid = 1005,
        [Description("The random parameter is missing")]
        OpSideRandomParameterMissing = 1006,
        [Description("Parameter(s) Incorrect")]
        OpSideParameterIncorrect = 1007,
        [Description("Duplicate request")]
        OpSideDuplicateRequest = 1009,
        [Description("Ticket Has Settled")]
        OpSideTicketHasSettled = 1010,
        [Description("The previous operation has not been completed")]
        OpSidePreviousTransactionNotCompleted = 1015,
        [Description("Login validation failed. Login name or password is incorrect")]
        OpSideLoginFailed = 2001,
        [Description("Account is locked")]
        OpSideAccountLocked = 2002,
        [Description("Account does not exist")]
        OpSideAccountNotExist = 2003,
        [Description("The IP address is restricted")]
        OpSideIpRestricted = 2004,
        [Description("Account is blacklisted")]
        OpSideAccountIsBlacklisted = 2005,
        [Description("Already processed with different details")]
        OpSideProcessedWithDifferentDetails = 3001,
        [Description("Player has insufficient funds")]
        OpSidePlayerHasInsufficientFunds = 3002,
        [Description("The player exceeded their game limit")]
        OpSidePlayerExceededGameLimit = 3003,
        [Description("The player is not permitted to play this game")]
        OpSidePlayerNoPermitted = 3004,
        [Description("The external system name does not exist (gamereference)")]
        OpSideExternalSystemNameNotExist = 3005,
        [Description("Win limit hit! Please contact your upline, thank you.")]
        OpSidePlayerExceededWinLimit = 3006,
        [Description("Lose limit hit! Please contact your upline, thank you.")]
        OpSidePlayerExceededLoseLimit = 3007,
        [Description("Member is enabled this product but he/she has no bet limit setting from upline")]
        OpSideNoBetLimitSetting = 3008,
        [Description("Member placing bet and uplines changes the betting limit on member")]
        OpSideBettingLimitHasChanges = 3009,
        [Description("All your current bets have been rejected")]
        OpSideCurrentBetsHasRejected = 3010,
        [Description("Your commission has been changed, please re-login to apply the new setting")]
        OpSideCommissionHasChanged = 3011,
        [Description("Your PT has been changed, please re-login to apply the new setting.")]
        OpSidePtHasChanged = 3012,
        [Description("This transaction can't be processed, due to the previous request has failed")]
        OpSidePreviousRequestHasFailed = 3013,
        [Description("The player exceeded company win limit")]
        OpSidePlayerExceededCompanyWinLimit = 3014,
        [Description("Settle Amount Incorrect")]
        OpSideSettleAmountIncorrect = 3015,
        [Description("No Such Ticket")]
        OpSideNoSuchTicket = 3016,
        [Description("Duplicate Transaction")]
        OpSideDuplicateTransaction = 3017,
        [Description("The player exceeded cash-in limit")]
        OpSidePlayerExceededCashInLimit = 3018,
        [Description("Total bets exceeded the table maximum in this round. Please wait for the next round.")]
        OpSideGameRoundBetLimitExceeded = 3019,
        [Description("Transaction failed")]
        OpSideTransactionFailed = 3020,
        [Description("Ticket Has Been Settled")]
        OpSideTransactionOpSideTicketHasBeenSettledFailed = 3021,
        [Description("Not Allowed Currency")]
        OpSideNotAllowedCurrency = 3022,
        [Description("Invalid Commission")]
        OpSideInvalidCommission = 3023,
        [Description("Not Allowed Country")]
        OpSideNotAllowedCountry = 3024,
        [Description("Player error")]
        OpSidePlayerError = 4001,
        [Description("Currency not found")]
        OpSideCurrencyNotFound = 4002,
        [Description("Setting not found")]
        OpSideSettingNotFound = 4003,
        [Description("Setting error")]
        OpSideSettingError = 4004,
        [Description("Unable to determine IP")]
        OpSideUnableToDetermineIp = 4006,
        [Description("IP not authorized")]
        OpSideIpNotAuthorized = 4007,
        [Description("Game not found")]
        OpSideGameNotFound = 4008,
        [Description("Not supported product")]
        OpSideNotSupportedProduct = 4009,
        [Description("Not supported casino type")]
        OpSideNotSupportedCasinoType = 4010,
        [Description("Not supported language")]
        OpSideNotSupportedLanguage = 4011,
        [Description("Member/User not found")]
        OpSideMemberOrUserNotFound = 4012,
        [Description("Network error")]
        OpSideNetworkError = 5002,
        [Description("Under maintenance")]
        OpSideUnderMaintenance = 5003,
        [Description("DB error")]
        OpSideDbError = 5004,
        [Description("Invalid AccessKey")]
        InvalidAccessKey = 7000,
        [Description("Invalid AccessToken")]
        InvalidAccessToken = 7001,
        [Description("Parameter Incorrect")]
        ParameterIncorrect = 7002,
        [Description("Wallet Operator Not Found")]
        WalletOperatorNotFound = 7003,
        [Description("Game Vendor Not Found")]
        GameVendorNotFound = 7004,
        [Description("Game Not Found")]
        GameNotFound = 7005,
        [Description("Setting Not Found")]
        SettingNotFound = 7006,
        [Description("Redis Exception")]
        RedisException = 7007,
        [Description("Ticket Not Exist")]
        TicketNotExist = 7008,
        [Description("Agent Setting Not Found")]
        AgentSettingNotFound = 7009,
        [Description("Exception Undefined")]
        ExceptionUndefined = 7999,
    }

    #endregion
}
