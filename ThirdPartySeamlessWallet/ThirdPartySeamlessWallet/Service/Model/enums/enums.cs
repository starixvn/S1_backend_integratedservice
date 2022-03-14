using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        Settled = 1, /* When Worker done */
        EndGame = 2 /* For Worker Condiction To Reach */
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
        V8_Poker = 42,
        CQ9_Fish = 44,
        N2Live_LiveCasino = 45,
        PT_RNG = 46,
        V8_Poker_None = 47, // without revenue games
        PT_LiveCasino = 48,
        BTI_Sports = 49,
        AMB_Poker = 50,
        NineW_Sports = 51,
        KM_Poker = 52,
        JILI_RNG = 53,
        TCG_SEA_Lottery=54,
        TCG_VN_Lottery =55,
        JILI_Fish = 56,
        MG_RNG = 57,
        MG_LiveCasino = 58,
        MP_Poker = 60,
        MP_Poker_None = 61, // without revenue games
        EVO_LiveCasino = 63,
        Netent_RNG = 64,
        RedTiger_RNG = 65,
        Joker_Fish = 66,
        AP_RNG = 67,
        HB_RNG = 68,
        AMB_RNG = 76
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
        CQ9 = 16,
        CF = 17,
        EBet = 18,
        AWC = 19,
        WM = 20,
        Kiss918 = 21,
        Mega = 22,
        One789 = 23,
        Iconic = 24,
        V8 = 25,
        PT = 27,
        N2Live = 28,
        BTI = 29,
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
        HB = 40
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
        Reserve = 0,
        Bet = 1,
        CancelBet = 2,
        Payout = 4,
        Tip = 6,
        Bonus = 7,
        Jackpot = 8,
        ConfirmReserve = 9,
        EndRound = 10, //回合結束
    }
    public enum AllowNegative
    {
        Allow = 1,
        NotAllow = 0
    }
    public enum TpCategory
    {
        Sports = 1,
        Slots = 2,
        LiveCasino = 3,
        Fish = 4,
        Cockfighting = 5,
        Lottery = 6,
        VirtualSports = 7,
        Poker = 8,
    }

    public enum PromotionCategory
    {
        SlotsGame = 1,
        LiveCasino = 2,
        Fish = 3,
        SabaSports = 4,
        Lottery = 5,
        Sports = 6,
        PTSlotsGame = 7,
        PTLiveCasino = 8
    }

    public enum Direction
    {
        ToTp = 1,
        ToMain = 2
    }

    public enum DataBase
    {
        Main, //ConnectionString
        Wager,//ConnectionStringWager
    }

    public enum SabaOnlineMemberStatus
    {
        [Description("Member Is Online")]
        MemberIsOnline = 0,
        [Description("Invalid Secret Key or Ip Address Not Allow")]
        InvalidSecretKeyOrIpAddressNotAllow = 1,
        [Description("Vendor MemberId NotFound")]
        VendorMemberIdNotFound = 2,
        [Description("Member Is Offline")]
        MemberIsOffline = 3
    }

    public enum Site
    {
        Nova88 = 1,
        Goal123 = 2,
        Bwing = 3
    }

    public enum TpCheckRecordCode
    {
        InsufficientBalance = 1,
    }
}
