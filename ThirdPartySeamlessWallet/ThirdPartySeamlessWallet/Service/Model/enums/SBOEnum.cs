using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum SBOResultType
    {
        Win = 0,
        Lose = 1,
        Tie = 2
    }

    public enum SBOProductType
    {
        [Description("体育博彩")]
        SportsBook = 1,
        [Description("电子游戏")]
        SBOGames = 3,
        [Description("虚拟运动")]
        VirtualSports = 5,
        [Description("真人赌场")]
        SBOLiveCasino = 7,
        [Description("无缝游戏")]
        SeamlessGameProvider = 9,
        [Description("線上貨幣")]
        LiveCoin = 10
    }

    public enum SBOErrorCode
    {
        [Description("No Error")]
        NoError = 0,
        [Description("Member not exist")]
        MemberNotExist = 1,
        [Description("Invalid Ip")]
        InvalidIp = 2,
        [Description("Username empty")]
        UsernameEmpty = 3,
        [Description("CompanyKey Error")]
        CompanyKeyError = 4,
        [Description("Not enough balance")]
        NotEnoughBalance = 5,
        [Description("Bet not exists")]
        BetNotExists = 6,
        [Description("Internal Error")]
        InternalError = 7,
        [Description("Bet Already Settled")]
        BetAlreadySettled = 2001,
        [Description("Bet Already Canceled")]
        BetAlreadyCanceled = 2002,
        [Description("Bet Already Rollback")]
        BetAlreadyRollback = 2003,
        [Description("Bet With Same RefNo Exists")]
        BetWithSameRefNoExists = 5003,
        [Description("Bet Already Returned Stake")]
        BetAlreadyReturnedStake = 5008
    }
}