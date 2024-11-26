using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BetRecordInfo
    {
        public int TpId { get; set; }
        public string TransId { get; set; }
        public GameType GameType { get; set; }
        public string TpUserName { get; set; }
        public long SettledTime { get; set; }
        public long TransTime { get; set; }
        public decimal Stake { get; set; }
        public decimal? ValidBet { get; set; }
        public decimal? Revenue { get; set; }
        public decimal ProgressiveBet { get; set; }
        public decimal ProgressiveWin { get; set; }
        public decimal WinlostAmount { get; set; }
        public SettlementType SettlementType { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public string OriginalState { get; set; }
        public int TicketKey { get; set; }
        public int BonusId { get; set; }
        /// <summary>
        /// 可用此單號來取回TpSeamlessBonus存放的Bonus紀錄
        /// </summary>
        public string ReferenceNo { get; set; } = string.Empty;
        public string GameID
        {
            get
            {
                return _gameID;
            }
            set
            {
                //DB欄位長度限制為50，限制長度為50避免造成結算錯誤
                _gameID = (value.Length > 50) ? value.Substring(0, 50) : value;
            }
        }
        private string _gameID = string.Empty;
    }

    public class SettleData
    {
        public string TransId { get; set; }
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinLostAmount { get; set; }
        public decimal? Revenue { get; set; }
        public string Account { get; set; }
        public long SettleTime { get; set; }
        public long TransTime { get; set; }
        public decimal ProgressiveBet { get; set; }
        public decimal ProgressiveWin { get; set; }
        public string GameId { get; set; } = string.Empty;
        public int TicketStatus { get; set; }
        public int TicketType { get; set; }
    }

    public class TpRewardRecordInfo
    {
        public int TpId { get; set; }
        public string TransId { get; set; }
        public GameType GameType { get; set; }
        public string TpUserName { get; set; }
        public long SettledTime { get; set; }
        public long TransTime { get; set; }
        public decimal Amount { get; set; }
        public SettlementType SettlementType { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public int BonusId { get; set; }
    }

    public class RewardData
    {
        public string TransId { get; set; }
        public decimal Amount { get; set; }
        public string Account { get; set; }
        public long SettledTime { get; set; }
        public long TransTime { get; set; }
        public string GameId { get; set; }
        public int TicketStatus { get; set; }
    }
}
