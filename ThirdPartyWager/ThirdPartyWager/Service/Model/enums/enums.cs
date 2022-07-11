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
        Iconic_RNG = 39,
        Iconic_Fish = 40,
        MP_Poker = 60,
        MP_Poker_None = 61, // without revenue games
        SG_RNG = 15,
        SG_Fish = 31,
        SBO_Sports = 77,
        SBO_VirtualSports = 81,
        YGG_RNG = 14,
 		PT_RNG = 46,
        PT_LiveCasino = 48        
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

    public enum WagerType
    {
        Wager = 1,//下注紀錄
        WagerReSettle = 2,//活動總金額
    }

    public enum WagerTimerType
    {
        Wager = 1,//正常抄單
        RedoWager = 2,//重作抄單
        Finance = 4,//廠商報表
    }
}
