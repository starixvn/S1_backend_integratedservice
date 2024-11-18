using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.MaxGame;

namespace ThirdPartySeamlessWallet.Service.Model.MaxGame
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
        public string GameType { get; set; }
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
        public List<BetDataSet_PlaceBetReq_MaxGame> BetDataSet { get; set; }
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
        public Dictionary<string,string> OtherBetInfo { get; set; }
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
        public string ResultId { get; set; }
        public DateTime SettleDate { get; set; }
        public long SettleDateUnix
        {
            get
            {
                return new DateTimeOffset(SettleDate, new TimeSpan(-4, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public Dictionary<string, string>? OtherBetInfo { get; set; }
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

    public class FreeSpinData_MaxGame
    {
        public string Timestamp { get; set; }
        public string Seq { get; set; }
        public Payload_FreeSpinReq_MaxGame Payload { get; set; }
    }

    public class Payload_FreeSpinReq_MaxGame
    {
        public string OperatorCustId { get; set; }
        public string GameType { get; set; }
        public string IpAddress { get; set; }
        public List<BetDataSet_FreeSpinReq_MaxGame> BetDataSet { get; set; }
    }

    public class BetDataSet_FreeSpinReq_MaxGame
    {
        public string TicketNo { get; set; }
        public string GameId { get; set; }
        public string GameRoundId { get; set; }
        public string TransactionId { get; set; }
        public decimal Stake { get; set; }
        public decimal ActualStake { get; set; }
        public decimal NetStake { get; set; }
        public decimal Payout { get; set; }
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

    public class SpNameListOfMaxGame
    {
        public string GetTicketNo { get; set; }
        public string InsertBettrans { get; set; }
        public string InsertPayload { get; set; }
        public string SetCancelBet { get; set; }
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

    public class BaseResp
    {
        public string Seq { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string TimeStamp { get; set; }

        public BaseResp(ErrorCodeOfMaxGame errorCode)
        {
            Seq = Guid.NewGuid().ToString();
            ErrorCode = (int)errorCode;
            ErrorMessage = errorCode.GetEnumDescription();
            TimeStamp = DateTime.UtcNow.ToString("O");
        }

        public BaseResp(ErrorCodeOfMaxGame errorCode, string seq)
        {
            Seq = seq;
            ErrorCode = (int)errorCode;
            ErrorMessage = errorCode.GetEnumDescription();
            TimeStamp = DateTime.UtcNow.ToString("O");
        }
    }

    public class CommonReq
    {
        public string Seq { get; set; }
        public string TimeStamp { get; set; }
    }

    public class Limit
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }
    }


    public class GetPlayerInfoResp : BaseResp
    {
        public GetPlayerInfoResp(ErrorCodeOfMaxGame errorCode, string seq) : base(errorCode, seq)
        {
        }

        public Payload_GetPlayerInfoResp Payload { get; set; }
    }

    public class Payload_GetPlayerInfoResp
    {
        private decimal _Balance = 0m;
        private decimal _MinBet = 0m;
        private decimal _MaxBet = 0m;
        public string OperatorCustId { get; set; }
        public string UserName { get; set; }
        public string CurrencyISO { get; set; }
        public string Currency { get; set; }
        public decimal Balance
        {
            get
            {
                _ = decimal.TryParse(_Balance.ToString("N2"), out _Balance);
                return _Balance;
            }
            set
            {
                _Balance = value;
            }
        }
        public decimal MinBet
        {
            get
            {
                _ = decimal.TryParse(_MinBet.ToString("N2"), out _MinBet);
                return _MinBet;
            }
            set
            {
                _MinBet = value;
            }
        }
        public decimal MaxBet
        {
            get
            {
                _ = decimal.TryParse(_MaxBet.ToString("N2"), out _MaxBet);
                return _MaxBet;
            }
            set
            {
                _MaxBet = value;
            }
        }
        public string BetGroup { get; set; }
        public string WalletSideToken { get; set; }
    }

    public class GetBalanceResp : BaseResp
    {
        public GetBalanceResp(ErrorCodeOfMaxGame errorCode, string seq) : base(errorCode, seq)
        {
        }
        public Payload_GetBalanceResp Payload { get; set; }
    }

    public class Payload_GetBalanceResp
    {
        private decimal _Balance = 0m;
        public decimal Balance
        {
            get
            {
                _ = decimal.TryParse(_Balance.ToString("N2"), out _Balance);
                return _Balance;
            }
            set
            {
                _Balance = value;
            }
        }
    }

    public class PlaceBetResp : BaseResp
    {
        public PlaceBetResp(ErrorCodeOfMaxGame errorCode, string seq) : base(errorCode, seq)
        {
        }

        public Payload_PlaceBetResp Payload { get; set; }
    }

    public class Payload_PlaceBetResp
    {
        private decimal _Balance = 0m;
        public string OperatorCustId { get; set; }
        public decimal Balance
        {
            get
            {
                _ = decimal.TryParse(_Balance.ToString("N2"), out _Balance);
                return _Balance;
            }
            set
            {
                _Balance = value;
            }
        }
        public List<BetDataSetList_MaxGame> BetDataSet { get; set; }
    }

    public class EndGameResp : BaseResp
    {
        public EndGameResp(ErrorCodeOfMaxGame errorCode, string seq) : base(errorCode, seq)
        {
        }
        public Payload_EndGameResp Payload { get; set; }
    }

    public class Payload_EndGameResp
    {
        private decimal _Balance = 0m;
        public string OperatorCustId { get; set; }
        public decimal Balance
        {
            get
            {
                _ = decimal.TryParse(_Balance.ToString("N2"), out _Balance);
                return _Balance;
            }
            set
            {
                _Balance = value;
            }
        }
    }

    public class Payload_PlaceBetAndSettleResp : Payload_EndGameResp
    {
        public List<BetDataSetList_MaxGame> BetDataSet { get; set; }
    }

    public class VoidGameResp : EndGameResp
    {
        public VoidGameResp(ErrorCodeOfMaxGame errorCode, string seq) : base(errorCode, seq)
        {
        }
    }

    public class BonusResp : BaseResp
    {
        public BonusResp(ErrorCodeOfMaxGame errorCode, string seq) : base(errorCode, seq)
        {
        }
        public Payload_BonusResp Payload { get; set; }
    }

    public class Payload_BonusResp
    {
        private decimal _Balance = 0m;
        public string OperatorCustId { get; set; }
        public decimal Balance
        {
            get
            {
                _ = decimal.TryParse(_Balance.ToString("N2"), out _Balance);
                return _Balance;
            }
            set
            {
                _Balance = value;
            }
        }
        public string RefNo { get; set; }
    }

    public class FreeSpinResp : BaseResp
    {
        public FreeSpinResp(ErrorCodeOfMaxGame errorCode, string seq) : base(errorCode, seq)
        {
        }

        public Payload_FreeSpinResp Payload { get; set; }
    }

    public class Payload_FreeSpinResp
    {
        private decimal _Balance = 0m;
        public string OperatorCustId { get; set; }
        public decimal Balance
        {
            get
            {
                _ = decimal.TryParse(_Balance.ToString("N2"), out _Balance);
                return _Balance;
            }
            set
            {
                _Balance = value;
            }
        }
        public List<BetDataSetList_MaxGame> BetDataSet { get; set; }
    }
}
