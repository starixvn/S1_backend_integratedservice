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
        Keno_Lottery = 5,
        Allbet = 6,
        PP = 7
    }
}
