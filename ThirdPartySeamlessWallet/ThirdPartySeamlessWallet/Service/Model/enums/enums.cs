using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum TransType/*Finical Type*/
    {
        Wagers = 1,
        Refunds = 2,
        Payout = 3,
        Prize = 4,
        Undo = 5,
        Tip = 6
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
    public enum SettleFlag
    {
        Running = 0, /* Status means StartGame(Bet) Or Running */
        Reached = 1, /* When Worker done */
        ReadyToReach = 2 /* For Worker Condiction To Reach */
    }

    public enum GameType
    {
        Saba_Sports = 1,
        Saba_NumberGame = 2,
        Saba_VirtualSport = 3,
        Saba_RNG = 4,
        Keno_Lottery = 5,
        Allbet_LiveCasino = 6,
        PP_RNG = 7,
        AG_LiveCasino = 8,
        SA_LiveCasino = 9,
        DG_LiveCasino = 10,
        GPI_RNG = 11,
        YL_Fish = 12,
        Joker_RNG = 13,
        YGG_RNG = 14,
        SG_RNG = 15,
        PP_LiveCasino = 16,
        PG_RNG = 17,
        Sexy_LiveCasino = 18,
        CF_Rooster = 27,
        EBet_LiveCasino = 28,
        CQ9_RNG = 29,
        AWC_Rooster = 30,
        SG_Fish = 31,
        WM_LiveCasino = 32,
        Kiss918_RNG = 33,
        Mega_RNG = 34,
        One789_Lottery = 35,
        One789_Lottery_789 = 36,
        One789_LiveCasino = 37,
        Iconic_RNG = 39,
        Iconic_Fish = 40,
        OneGaming_Poker = 41,
        V8_Poker = 42
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
        Allbet = 12,
        OneGaming = 13,
        PG = 14,
        Sexy = 15,
        CQ9 = 16,
        CF = 17,
        EBet = 18,
        AWC = 19,
        WM = 20,
        Kiss918 = 21,
        Mega = 22,
        One789 = 23,
        Iconic = 24,
        V8 = 25
    }
    public enum TransferType : int
    {
        PlaceBet = 1,
        CancelBet = 2,
        Payout = 4,
        Bonus = 7,
        Tip = 6
    }
    public enum BettransType : int/*Ticket Type*/
    {
        Bet = 1,
        CancelBet = 2,
        Payout = 4,
        Tip = 6,
        Bonus = 7,
        Jackpot = 8
    }
    public enum AllowNegative
    {
        Allow = 1,
        NotAllow = 0
    }
    public enum TpCategory
    {
        Slots,
        LiveCasino,
        Fish,
        Lottery,
        Poker
    }
    public enum Direction
    {
        ToTp = 1,
        ToMain = 2
    }
}
