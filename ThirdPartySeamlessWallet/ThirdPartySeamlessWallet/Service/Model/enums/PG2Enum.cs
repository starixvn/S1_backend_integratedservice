using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums;

public enum PG2ErrorCode
{
    [Description("Success")]
    Success = 0,
    [Description("Balance insufficient")]
    BalanceInsufficient = 800,
    [Description("Duplicate Round Id")]
    DuplicateRound = 806, // old
    [Description("Transaction already canceled")]
    AlreadyCanceled = 807,
    [Description("Duplicate round id")]
    DuplicateRoundId = 900,
    [Description("Invalid username or token")]
    InvalidUsernameOrToken = 908,
    [Description("Invalid game id")]
    InvalidGameId = 995,
    [Description("Invalid request data")]
    InvalidRequest = 997,
    [Description("Invalid agent id")]
    InvalidAgent = 998,
    [Description("Service not available")]
    ServiceNotAvailable = 999,
    [Description("Response error via axios timeout")]
    ResponseTimeout = 1601
}