using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class GetPlayerInfoReq_MaxGame
    {
        public string Timestamp { get; set; }
        public string Seq { get; set; }
        public Payload_GetPlayerInfoReq_MaxGame Payload { get; set; }
    }

    public class Payload_GetPlayerInfoReq_MaxGame
    {
        public string OperatorCustId { get; set; }
        public string GameType { get; set; }
        public string GameId { get; set; }
        public string WalletSideToken { get; set; }
    }

    public class GetBalanceReq_MaxGame
    {
        public string Timestamp { get; set; }
        public string Seq { get; set; }
        public Payload_GetBalanceReq_MaxGame Payload { get; set; }
    }

    public class Payload_GetBalanceReq_MaxGame
    {
        public string OperatorCustId { get; set; }
        public string WalletSideToken { get; set; }
    }

    public class BettingData_MaxGame
    {
        public string Timestamp { get; set; }
        public string Seq { get; set; }
        public Payload_PlaceBetReq_MaxGame Payload { get; set; }
    }

    public class Payload_PlaceBetReq_MaxGame
    {
        public string OperatorCustId { get; set; }
        public string WalletSideToken { get; set; }
        public string GameType { get; set; }
        public string IpAddress { get; set; }
        public DateTime TransDate { get; set; }
        public List<BetDataSet_PlaceBetReq_MaxGame> BetDataSet { get; set; }
        public long TransDateUnix
        {
            get
            {
                return new DateTimeOffset(TransDate, new TimeSpan(-4, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class BetDataSet_PlaceBetReq_MaxGame
    {
        public string TicketNo { get; set; }
        public string GameId { get; set; }
        public string GameRoundId { get; set; }
        public string TransactionId { get; set; }
        public decimal Stake { get; set; }
        public decimal ActualStake { get; set; }
        public decimal Odds { get; set; }
        public string BetChoice { get; set; }
        public string TableCode { get; set; }
    }

    public class PayoutData_MaxGame
    {
        public string Timestamp { get; set; }
        public string Seq { get; set; }
        public Payload_EndGameReq_MaxGame Payload { get; set; }
    }

    public class Payload_EndGameReq_MaxGame
    {
        public string OperatorCustId { get; set; }
        public string GameType { get; set; }
        public List<BetDataSet_EndGameReq_MaxGame> BetDataSet { get; set; }
    }

    public class BetDataSet_EndGameReq_MaxGame
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
        public long SettleDateUnix
        {
            get
            {
                return new DateTimeOffset(SettleDate, new TimeSpan(-4, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class CancelBetData_MaxGame
    {
        public string Timestamp { get; set; }
        public string Seq { get; set; }
        public Payload_VoidGameReq_MaxGame Payload { get; set; }
    }

    public class Payload_VoidGameReq_MaxGame
    {
        public string OperatorCustId { get; set; }
        public string GameType { get; set; }
        public List<BetDataSet_VoidGameReq_MaxGame> BetDataSet { get; set; }
    }

    public class BetDataSet_VoidGameReq_MaxGame
    {
        public string TicketNo { get; set; }
        public string GameId { get; set; }
        public string GameRoundId { get; set; }
        public string TransactionId { get; set; }
        public decimal Stake { get; set; }
        public decimal ActualStake { get; set; }
    }

    public class BonusData_MaxGame
    {
        public string Timestamp { get; set; }
        public string Seq { get; set; }
        public Payload_BonusReq_MaxGame Payload { get; set; }
    }

    public class Payload_BonusReq_MaxGame
    {
        public string OperatorCustId { get; set; }
        public string GameType { get; set; }
        public List<BetDataSet_BonusReq_MaxGame> BetDataSet { get; set; }
    }

    public class BetDataSet_BonusReq_MaxGame
    {
        public string TicketNo { get; set; }
        public string GameId { get; set; }
        public string GameRoundId { get; set; }
        public string TransactionId { get; set; }
        public decimal Payout { get; set; }
        public string CurrencyISO { get; set; }
        public string BonusId { get; set; }
        public string BonusName { get; set; }
        public string PrizeName { get; set; }
        public DateTime SettleDate { get; set; }
        public long SettleDateUnix
        {
            get
            {
                return new DateTimeOffset(SettleDate, new TimeSpan(-4, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class MaxGameConfig : TpSiteConfig
    {
        public string OperatorPwd { get; set; }
        public string OperatorKey { get; set; }
        public string GameType { get; set; }
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
        public string TransactionId { get; set; }
        public decimal Stake { get; set; }
        public decimal ActualStake { get; set; }
        public decimal NetStake { get; set; }
        public decimal Payout { get; set; }
        public decimal Odds { get; set; }
        public string CurrencyISO { get; set; }
        public BettransType TicketType { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public SettleFlag SettleFlag { get; set; }
    }

    public class BetDataSetList_MaxGame
    {
        public string TicketNo { get; set; }
        public string RefNo { get; set; }
    }
}
