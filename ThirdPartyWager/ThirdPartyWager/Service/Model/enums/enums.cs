namespace ThirdPartyWager.Service.Model.enums
{
    public enum Tp
    {
        None = 0,
        Saba = 1,
        SA = 2,
        AG = 3,
        DG = 4,
        PP = 5,
        GPI = 6,
        YL = 7,
        YGG = 8,
        Joker = 9,
        SG = 10,
        KM = 11,
        Allbet = 12,
        OneGaming = 13,
        PG = 14,
        Sexy = 15,
        CF = 17,
        WM = 20,
        Kiss918 = 21,
        Mega = 22,
        One789 = 23,
        Iconic = 24,
        V8 = 25,
        PT = 27,
        N2Live = 28,
        NineW = 31,
        TCG = 33,
        MG = 34,
        MP = 35,
        EVO = 36,
        Netent = 37,
        RedTiger = 38,
        SBO = 42
    }

    public enum GameType
    {
        Allbet_LiveCasino = 6,
        AG_LiveCasino = 8,
        SA_LiveCasino = 9,
        One789_Lottery = 35,
        One789_Lottery_789 = 36,
        One789_LiveCasino = 37,
        MP_Poker = 60,
        MP_Poker_None = 61, // without revenue games
        SG_RNG = 15,
        SG_Fish = 31,
        SBO_Sports = 77,
        SBO_VirtualSports = 81,
        YGG_RNG = 14
    }

    public enum DataBase
    {
        Main, //ConnectionString
        Report,//ConnectionStringReport
        Wager,//ConnectionStringWager
    }

    public enum SummaryType
    {
        TpBetRecord = 1,//下注紀錄
        TpRewardTotalAmount = 2,//活動總金額
        TpRewardFreeGame = 3,//活動免費遊戲
        TpRewardCash = 4,//活動現金獎勵
    }
}
