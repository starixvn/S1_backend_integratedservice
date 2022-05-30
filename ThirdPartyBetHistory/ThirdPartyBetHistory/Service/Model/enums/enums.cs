using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

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
        Saba_Sports = 1,
        Saba_NumberGame = 2,
        Saba_VirtualSport = 3,
        Saba_RNG = 4,
        Saba_Keno = 5,
        Allbet = 6,
        PP = 7,
        Saba_Lottery = 19,
        Saba_Indo4D = 20,
        Saba_CardClub = 21,
        Saba_VirtualGames = 22,
        Saba_TableGame = 23,
        Saba_VirtualSports2 = 24,
        Saba_SabaClub = 25,
        Saba_MacauGame = 26,
        Saba_ChinaLottery = 38,
        Saba_Vgaming = 59,
        Saba_Bitcoin = 62,
        SBSW_Sports = 69,
        SBSW_NumberGame = 70,
        SBSW_VirtualSports = 71,
        SBSW_VirtualSports2 = 72,
        SBSW_Keno = 73,
        SBSW_Lottery = 74,
        SBSW_TableGame = 75,
        Saba_LGVirtualSports = 80,
    }

    public enum Tp
    {
        Saba = 1,
        SBSW = 41
    }

    public enum SiteId
    {
        Nova88 = 1,
        Goal123 = 2,
        Bwing = 3
    }

    public enum OddsType
    {
        [Description("不属于任何一种市场盘口(仅出现于 sport_type=245，165，200，168，bet_type=468，469)")] SpecialOdds = 0,
        [Description("马来盘")] MalayOdds = 1,
        [Description("中国盘")] ChinaOdds = 2,
        [Description("欧洲盘")] DecimalOdds = 3,
        [Description("印度尼西亚盘")] IndoOdds = 4,
        [Description("美国盘")] AmericanOdds = 5
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
	
    public enum TimeType
    {
        ByBetTime = 1,
        BySettleTime = 2
    }

    public enum WorkerType
    {
        MainWager = 1,
        LGVirtualSportsWager = 2,
        ByTransIdWager = 3
    }
}
