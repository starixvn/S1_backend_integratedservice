using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    #region ErrorCode

    //1 Indicates FAIL, 0 indicates SUCCESS,
    public enum StatusOfDragonGaming
    {
        [Description("Fail")]
        Fail = 1,
        [Description("Success")]
        Success = 0,
    }

    public class ErrorIdInfo
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public static class ErrorIdExtensions
    {
        public static readonly Dictionary<int, ErrorIdInfo>ErrorIdDetails = new Dictionary<int, ErrorIdInfo>
        {
            { 2012, new ErrorIdInfo { Code = "INVALID_LOGIN_CREDENTIALS", Message = "Unable to login with credentials provided." } },
            { 2013, new ErrorIdInfo { Code = "INVALID_LOGIN_CREDENTIALS", Message = "Must include username and password." } },
            { 2014, new ErrorIdInfo { Code = "INVALID_LOGIN_CREDENTIALS", Message = "User does not belong to this website." } },
            { 2015, new ErrorIdInfo { Code = "INVALID_API_KEY", Message = "API Key is invalid" } },
            { 2016, new ErrorIdInfo { Code = "INVALID_SESSION_ID", Message = "Invalid session ID" } },
            { 2017, new ErrorIdInfo { Code = "INVALID_PROVIDER", Message = "Invalid provider" } },
            { 2018, new ErrorIdInfo { Code = "INVALID_GAME_TYPE", Message = "Invalid game type" } },
            { 2019, new ErrorIdInfo { Code = "INVALID_PLATFORM", Message = "Invalid platform" } },
            { 2020, new ErrorIdInfo { Code = "INVALID_AMOUNT_TYPE", Message = "Invalid amount type" } },
            { 2021, new ErrorIdInfo { Code = "INVALID_PLAYER_ID", Message = "Invalid player ID" } },
            { 2022, new ErrorIdInfo { Code = "INVALID_GAME_ID", Message = "Invalid game ID." } },
            { 2023, new ErrorIdInfo { Code = "INVALID_LANGUAGE", Message = "Invalid language." } },
            { 2024, new ErrorIdInfo { Code = "INVALID_USERNAME", Message = "Invalid username." } },
            { 2025, new ErrorIdInfo { Code = "CURRENT_NEW_PASSWORD_SAME", Message = "Current & new password should not be same." } },
            { 2026, new ErrorIdInfo { Code = "INVALID_CURRENCY", Message = "Invalid currency." } },
            { 2027, new ErrorIdInfo { Code = "NEGATIVE_AMOUNT_VALUE", Message = "Amount should be positive value." } },
            { 2028, new ErrorIdInfo { Code = "INSUFFICIENT_BALANCE", Message = "Insufficient balance." } },
            { 2029, new ErrorIdInfo { Code = "INVALID_STARTDATE_ENDDATE", Message = "Start date should not be greater than end date." } },
            { 2030, new ErrorIdInfo { Code = "INVALID_AMOUNT_VALUE", Message = "Amount should be less than balance." } },
            { 2031, new ErrorIdInfo { Code = "DUPLICATE_SESSION_ID", Message = "Duplicate session ID." } },
            { 2032, new ErrorIdInfo { Code = "OPERATOR_AUTHENTICATION_FAIL", Message = "Authentication with operator failed." } },
            { 2033, new ErrorIdInfo { Code = "CURRENCY_MISMATCH", Message = "Player currency mismatch between operator and opus" } },
            { 2034, new ErrorIdInfo { Code = "ZERO_AMOUNT_VALUE", Message = "Amount should be more than zero." } },
            { 2035, new ErrorIdInfo { Code = "FAILED_TRANS", Message = "Transaction not found or failed." } },
            { 2036, new ErrorIdInfo { Code = "INVALID_FROM_DATE", Message = "From date is greater than to date." } },
            { 2037, new ErrorIdInfo { Code = "INVALID_TIME_INTERVAL", Message = "Time interval is more than the allowed one." } },
            { 2038, new ErrorIdInfo { Code = "INVALID_MONETARY_TYPE", Message = "Invalid monetary type." } },
            { 2040, new ErrorIdInfo { Code = "INVALID_TRANSACTION_TYPE", Message = "Invalid transaction type. Ex: it should be either withdraw or deposit" } },
            { 2062, new ErrorIdInfo { Code = "DUPLICATE_USERNAME", Message = "Duplicate username." } },
        };

        public static ErrorIdInfo GetErrorIdInfo(this int id)
        {
            return ErrorIdDetails.TryGetValue(id, out var info) ? info : null;
        }
    }

    #endregion
}
