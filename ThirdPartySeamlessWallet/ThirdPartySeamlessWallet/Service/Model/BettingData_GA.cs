namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingData_GA
    {
        public string id { get; set; }
        public string member_ref_id { get; set; }
        public int type { get; set; }
        public string account_id { get; set; } = string.Empty;
        public string arena_code { get; set; } = string.Empty;
        public decimal bet_amount { get; set; } = 0;
        public decimal ticket_amount { get; set; } = 0;
        public decimal ante_amount { get; set; } = 0;
        public DateTime created_date_time { get; set; } = DateTime.Now;
        public int fight_number { get; set; } = 0;
        public string match_day_code { get; set; } = string.Empty;
        public DateTime modified_date_time { get; set; } = DateTime.Now;
        public decimal odds { get; set; } = 0;
        public string result { get; set; } = string.Empty;
        public string selection { get; set; } = string.Empty;
        public DateTime? settled_date_time { get; set; } = DateTime.Now;
        public string sid { get; set; } = string.Empty;
        public int status { get; set; } = 0;
        public decimal win_loss_amount { get; set; } = 0;
        public decimal amount { get; set; }
        public int TicketType { get; set; }
    }

    public class GAConfig : TpSiteConfig
    {
        public string PrivateKey { get; set; }
        public string PartnerAccountID { get; set; }
        public string GameClientID { get; set; }
        public string Token { get; set; }
    }
    public class GATransferReq
    {
        public string amount { get; set; }
        public string id { get; set; }
        public string member_ref_id { get; set; }
        public ticket ticket { get; set; }
        public int type { get; set; }
    }
    public class ticket
    {
        public string account_id { get; set; }
        public string bet_amount { get; set; }
        public string arena_code { get; set; }
        public string ticket_amount { get; set; }
        public string ante_amount { get; set; }
        public DateTime created_date_time { get; set; }
        public string currency_code { get; set; }
        public int fight_number { get; set; }
        public string match_day_code { get; set; }
        public string member_ref_id { get; set; }
        public DateTime modified_date_time { get; set; }
        public string odds { get; set; }
        public string result { get; set; }
        public string selection { get; set; }
        public DateTime? settled_date_time { get; set; }
        public string sid { get; set; }
        public int status { get; set; }
        public string win_loss_amount { get; set; }
    }

    public enum GATransactionStatus
    {
        Success = 0,
        Fail = 1,
        NoBet = 2,
        DuplicatePay = 3,
        DuplicateBet = 4,
        InsufficientBalance = 5,
        NoCancel = 6,
        BetIsCancel = 7
    }
    public class GAWager
    {
        public decimal bet_amount { get; set; }
        public decimal ticket_amount { get; set; }
        public decimal ante_amount { get; set; }
        public decimal win_loss_amount { get; set; }
        public string member_ref_id { get; set; }
        public string result { get; set; }
        public decimal odds { get; set; }
        public GAStatus status { get; set; }
        public string currency_code { get; set; }
    }
    public class SettleDataGA : BettingData_GA
    {
        public long unix_settled_date_time { get; set; }
        public long unix_created_date_time { get; set; }
    }
    public enum GAStatus
    {
        TicketIsConfirmed = 0,
        SettlementIsInProgress = 1,
        SettledButSsWaitingForPayout = 2,
        SettledButPayoutIsInProgress = 3,
        TicketIsSettled = 4,
        CanceledButIsWaitingForPayout = 5,
        CanceledButPayoutIsInProgress = 6,
        TicketIsCanceled = 7,
        VoidIsInProgress = 8,
        TicketIsVoided = 9,
        UnsettledButWaitingForPayout = 10,
        UnsettledButPayoutIsInProgress = 11,
        UncancelledWaitingForPayout = 12,
        UncancelledButPayoutIsInProgress = 13
    }
    public enum GAType
    {
        Bet = 0,
        TicketWinnings = 1,
        TicketLoss = 2,
        TicketDraw = 3,
        TicketCancelation = 4,
        TicketRollback = 6,
        Void = 9,
        Adjustment = 10,
        Unsettle = 11,
        Uncancel = 12,
        Jackpot = 13
    }
    public class BettingData_GA_Detail : BettingData_GA
    {
        public DateTime CreateTime { get; set; }
    }
}
