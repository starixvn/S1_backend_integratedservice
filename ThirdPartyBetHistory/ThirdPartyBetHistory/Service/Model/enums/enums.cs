using System.ComponentModel;

namespace ThirdPartyBetHistory.Service.Model.enums
{
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
        Refund = 3,/*Reject, Refund, Void*/
        Draw = 4/*Draw*/
    }

    public enum GameType
    {
        SBSW_Sports = 69,
        SBSW_NumberGame = 70,
        SBSW_VirtualSports = 71,
        SBSW_VirtualSports2 = 72,
        SBSW_Keno = 73,
        SBSW_Lottery = 74,
        SBSW_TableGame = 75,
        SBSW_Vgaming = 95,
        SBSW_CardClub = 96,
    }

    public enum Tp
    {
        SBSW = 41
    }

    public enum BetType
    {
        [Description("串关(其他)")] Parlay = 29,
        [Description("单场串关(足球)")] SingleMatchParlay = 38,
        [Description("比分让球盘(足球)")] FootballScoreHandicap = 468,
        [Description("比分大小盘(足球)")] FootballScoreOverUnder = 469
    }
    public enum DataBase
    {
        Main, //MainDB
        Report,//ReportDB
    }

    public enum WorkerType
    {
        MainWager = 1,                  //使用TicketKey固定持續拉單
        ByTransIdUnSettleWager = 3,     //檢查未結算注單
        ByTransIdSettleWager = 4,       //檢查二次結算注單
        RngWager = 5
    }

    public enum TpCategory
    {
        Sports = 1,
        Casino = 2,
        LiveCasino = 3,
        Fishing = 4,
        Cockfighting = 5,
        Lottery = 6,
        VirtualSports = 7,
        Poker = 8,
    }


}
