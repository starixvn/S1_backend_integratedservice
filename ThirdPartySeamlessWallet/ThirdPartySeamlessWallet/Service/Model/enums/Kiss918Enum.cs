using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum Kiss918ErrorCode
    {
        [Description("Success")]
        Success = 0,
        [Description("Balance insufficient")]
        BalanceInsufficient = 1,
        [Description("Player not exist")]
        InvaildPlayer = 2,
        [Description("Invalid bet")]
        InvaildBet = 3,
        [Description("Player auth fail")]
        InvaildPlayerAuth = 4,
        [Description("Player already locked")]
        PlayerLocked = 6,
        [Description("Game not exist or already disabled")]
        InvaildGame = 8,
        [Description("Internal server error")]
        ServiceNotAvailable = 100
    }
}
