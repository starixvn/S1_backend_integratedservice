using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.Model
{
    public enum TypeOfMaxGame
    {
        Running = 0,
        Win = 1,
        Lose = 2,
        Draw = 3,
        Void = 4,
        Reject = 5,
        Refund = 6,
    }

    public class CheckTicketStatusPayloadMaxGame
    {
        public string GameType { get; set; }
        public string OperatorCustId { get; set; }
        public string TicketNo { get; set; }
        public string TransDate { get; set; }
        public string AgentId { get; set; }
        public string AgentKey { get; set; }
        public string AgentCode { get; set; }
        public string ExtraInfo { get; set; }
    }

    public class TicketDataMaxGame
    {
        public string Seq { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime TimeStamp { get; set; }
        public PayloadMaxGame Payload { get; set; }
    }

    public class PayloadMaxGame
    {
        public string TicketNo { get; set; }
        public string OperatorCustId { get; set; }
        public string GameRoundId { get; set; }
        public string TransactionId { get; set; }
        public TypeOfMaxGame Status { get; set; }
        public decimal Stack { get; set; }
        public decimal ActualStake { get; set; }
        public decimal NetStake { get; set; }
        public decimal Payout { get; set; }
        public DateTime? SettleDate { get; set; } = new DateTime(1970, 1, 1);
        public string GameResult { get; set; }
        public decimal Odds { get; set; }
        public Dictionary<string,string> OtherBetInfo { get; set; } = null;
    }

    public class BettransData_MaxGame
    {
        public string Timestamp { get; set; }
        public string Seq { get; set; }
        public string OperatorCustId { get; set; }
        public string WalletSideToken { get; set; }
        public string GameType { get; set; }
        public string TicketNo { get; set; }
        public string GameId { get; set; }
        public string GameRoundId { get; set; }
        public string TransactionId { get; set; } = "";
        public decimal Stake { get; set; }
        public decimal ActualStake { get; set; }
        public decimal NetStake { get; set; }
        public decimal Payout { get; set; }
        public decimal Odds { get; set; }
        public string CurrencyISO { get; set; }
        public string ResultId { get; set; }
        public int SettleFlag { get; set; }
        public int TicketType { get; set; }
    }

    public class BettransData_JiLi
    {
        public string Timestamp { get; set; }
        public string Seq { get; set; }
        public string UserId { get; set; }
        public string WalletSideToken { get; set; }
        public string GameType { get; set; }
        public string TicketNo { get; set; }
        public string Game { get; set; }
        public string GameRoundId { get; set; }
        public string TransactionId { get; set; }
        public decimal Stake { get; set; }
        public decimal ActualStake { get; set; }
        public decimal NetStake { get; set; }
        public decimal Payout { get; set; }
        public decimal Odds { get; set; }
        public string CurrencyISO { get; set; }
        public string ResultId { get; set; }
        public int SettleFlag { get; set; }
        public int TicketType { get; set; }
        public string OperatorCustId => UserId;
        public string GameId => Game;
    }

    public class BetDataSetVoidGameReq_MaxGame
    {
        public string TicketNo { get; set; }
        public string GameId { get; set; }
        public string GameRoundId { get; set; }
        public string TransactionId { get; set; }
        public int Status { get; set; }
    }

    public class BetDataSetEndGameReq_MaxGame
    {
        public string TicketNo { get; set; }
        public string GameId { get; set; }
        public string GameRoundId { get; set; }
        public string TransactionId { get; set; }
        public decimal ActualStake { get; set; }
        public decimal NetStake { get; set; }
        public decimal Payout { get; set; }
        public decimal Odds { get; set; }
        public string GameResult { get; set; }
        public DateTime SettleDate { get; set; }
        public string ResultId { get; set; }
        public Dictionary<string,string> OtherBetInfo { get; set; }
    }

    public class SeamlessMaxGameResponse
    {
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PayloadRequestDailySummaryData_MaxGame {
        public string TargetDate { get; set; }
        public int? TimeZone { get; set; } = null;
        public string GameType { get; set; } = null;
        public IEnumerable<int> TicketStatus { get; set; } = Array.Empty<int>();
    }

    public class DailySummaryData_MaxGame {
        public string Seq { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime TimeStamp { get; set; }
        public PayloadDailySummaryData_MaxGame Payload { get; set; }
    }

    public class PayloadDailySummaryData_MaxGame {
        public IEnumerable<BettingDataByGameType_MaxGame> GameTypes { get; set; }
    }
    public class BettingDataByGameType_MaxGame {
        public string GameType { get; set; }
        public IEnumerable<BettingDataByCurrency_MaxGame> BettingDate { get; set; }
    }
    public class BettingDataByCurrency_MaxGame {
        public string CurrencyIso { get; set; }
        public int BetCount { get; set; }
        public decimal Stake { get; set; }
        public decimal ActualStake { get; set; }
        public decimal NetStake { get; set; }
        public decimal WinLose { get; set; }
        public int JackpotContributionBetCount { get; set; }
        public decimal JackpotContribution { get; set; }
        public int JackpotPrizeBetCount { get; set; }
        public decimal JackpotPrize { get; set; }
        public int PromotionPrizeBetCount { get; set; }
        public decimal PromotionPrize { get; set;}
    }

    public enum MaxGame_TicketStatus {
        Win = 1,
        Lose = 2,
        Draw = 3,
        Void = 4,
        Refund = 6
    }
}