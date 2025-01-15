namespace ThirdPartyWager.Service.Model.enums
{
    public enum Site
    {
        Nova88 = 1,
        Goal123 = 2,
        Bwing = 3
    }

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
        AESexy = 15,
        CQ9 = 16,
        CF = 17,
        WE = 18,
        WM = 20,
        Kiss918 = 21,
        Mega = 22,
        One789 = 23,
        Iconic = 24,
        V8 = 25,
        PT = 27,
        N2Live = 28,
        AMB = 30,
        NineW = 31,
        JILI = 32,
        TCG = 33,
        MG = 34,
        MP = 35,
        EVO = 36,
        Netent = 37,
        RedTiger = 38,
        AP = 39,
        SBO = 42,
        Skywind = 43,
        SimplePlay = 44,
        Yeebet = 46,
        BG = 47,
        TFG = 48,
        Nextspin = 50,
        PlayTech = 51,
        IM = 52,
        SPRIBE = 53,
        FaChai = 55,
        Pegasus = 56,
        BNG = 57,
        HaoLong = 58,
        NLC = 59,
        MaxGame = 61,
        NTConnect = 62,
        GA = 63,
        WonCasino = 64,
        SabaPlay = 66,
        UuSlots = 67,
        Live22 = 68,
        FastSpin = 69,
        DragonGaming = 70,
        RNGKeno = 71,
        Lottery = 72,
        TableGame = 73
    }

    public enum GameType
    {
        Allbet_LiveCasino = 6,
        PP_RNG = 7,
        AG_LiveCasino = 8,
        SA_LiveCasino = 9,
        GPI_RNG = 11,
        YGG_RNG = 14,
        SG_RNG = 15,
        PP_LiveCasino = 16,
        PG_RNG = 17,
        AESexy_LiveCasino = 18,
        WE_LiveCasino = 28, // WorldEntertainment
        SG_Fish = 31,
        WM_LiveCasino = 32,
        One789_Lottery = 35,
        One789_Lottery_789 = 36,
        One789_LiveCasino = 37,
        Iconic_RNG = 39,
        Iconic_Fish = 40,
        V8_Poker = 42,
        PT_RNG = 46,
        V8_Poker_None = 47, // without revenue games
        PT_LiveCasino = 48,
        AMB_Poker = 50,
        KM_Poker = 52,
        JILI_RNG = 53,
        MP_Poker = 60,
        MP_Poker_None = 61, // without revenue games
        SBO_Sports = 77,
        SBO_VirtualSports = 81,
        AMB_RNG = 76,
        Skywind_RNG = 82,
        Skywind_LiveCasino = 84,
        Skywind_Fish = 85,
        SimplePlay_RNG = 86,
        GPI_Keno = 88,
        GPI_Sode = 89,
        GPI_Thai_Lottery = 90,
        GPI_Fast3 = 91,
        Yeebet_LiveCasino = 93,
        BG_LiveCasino = 94,
        TFG_Esports = 97,
        IM_ESports = 102,
        IM_Sports = 103,
        FaChai_RNG = 106,
        SG_Table = 109,
        HaoLong_4D = 110,
        NLC_RNG = 111,
        MaxGame_Poker = 113,
        NTConnect_Games = 114,
        GA_CockFighting = 115,
        WonCasino_LiveCasino = 116,
        SabaPlay_Poker = 119,
        UuSlots_RNG = 120,
        Live22_RNG = 121,
        FastSpin_RNG = 122,
        RNG_Keno = 124,
        Lottery = 125,
        TableGame = 126,
        Nextspin_RNG = 127
    }

    public enum SettlementType
    {
        UnSettled = 0,
        SettledForValid = 1,
        SettledForInvalid = 2,
        SettledForValidNotRollover = 3,
        Cheating = 4
    }

    public enum TicketStatus
    {
        UnSettled = 0,/*Running, Waiting*/
        Win = 1,
        Lost = 2,
        Refund = 3,/*Reject, Refund, Void, Draw*/
        Draw = 4,
        Related = 5,
        FreeBetWin = 6
    }

    public enum DataBase
    {
        Main, //ConnectionString
        Wager,//ConnectionStringWager
    }

    public enum SummaryType
    {
        TpBetRecord = 1,//下注紀錄
        TpRewardTotalAmount = 2,//活動總金額
        TpRewardFreeGame = 3,//活動免費遊戲
        TpRewardCash = 4,//活動現金獎勵
    }

    public enum WagerType
    {
        Wager = 1,//下注紀錄
        WagerReSettle = 2,//活動總金額
    }

    public enum WagerTimerType
    {
        Wager = 1,//正常抄單
        RedoWager = 2,//重作抄單
        CheckReward = 3,//活動檢查
        Finance = 4,//廠商報表
        CheckSettle = 5,//結帳檢查
        ReSettle = 6,//重新結帳
        UserReport = 7,//用戶報表
        UnSettle = 8,//未結算注單
        Cancel = 9,//已取消注單
    }

    public enum WorkerType
    {
        MainWager = 1,                  //使用TicketKey固定持續拉單
        ByTransIdUnSettleWager = 3,     //檢查未結算注單
        RngWager = 5,
        VgamingUnSettle = 6,            //檢查Vgaming未結算，重打Saba Resubmit Process
    }
}