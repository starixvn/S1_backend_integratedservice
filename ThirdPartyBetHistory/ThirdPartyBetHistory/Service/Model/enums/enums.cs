using System;
using System.Collections.Generic;
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
        Saba_Vgaming = 59
    }

    public enum Tp
    {
        Saba = 1
    }
    
    public enum SiteId
    {
        Nova88 = 1,
        Goal123 = 2,
        Bwing = 3
    }
}
