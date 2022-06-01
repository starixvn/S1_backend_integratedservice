using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum JokerErrorCode
    {
        [Description("Success")]
        Success = 0,
        [Description("IP not allowed")]
        IpNotAllowed = 1,
        [Description("Invalid AppID")]
        InvalidAppId = 2,
        [Description("Invalid token")]
        InvalidToken = 3,
        [Description("Invalid parameters")]
        InvalidParameters = 4,
        [Description("Invalid signature")]
        InvalidSignature = 5,
        [Description("Invalid timestamp")]
        InvalidTimestamp = 6,
        [Description("Invalid UserName or Password")]
        InvalidUserNameOrPassword = 7,
        [Description("Insufficient fund")]
        InsufficientFund = 100,
        [Description("Transaction is being processed")]
        AlreadyProcessed = 201,
        [Description("Server under maintenance")]
        UnderMaintenance = 999,
        [Description("Other")]
        Other = 1000
    }

    public enum RewardRNG_JokerTypeEnum
    {
        FromJoin = 1,
        FromCancel = 3,
        FromWin = 4
    }
}
