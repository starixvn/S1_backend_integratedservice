using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{    
    public enum AMBErrorCode
    {
        [Description("Success")]
        Success = 0,
        [Description("Balance insufficient")]
        BalanceInsufficient = 800,
        [Description("Duplicate Round Id")]
        DuplicateRound = 806,
        [Description("Transaction already canceled")]
        AlreadyCanceled = 807,
        [Description("Invalid request data")]
        InvalidRequest = 997,
        [Description("Invalid agent id")]
        InvalidAgent = 998,
        [Description("Service not available")]
        ServiceNotAvailable = 999,
        [Description("Response error via axios timeout")]
        ResponseTimeout = 1601
    }

    public enum BettransRng_AMBTypeEnum
    {
        FromBet = 1,
        FromPayout = 4
    }
}
