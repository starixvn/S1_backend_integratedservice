namespace ThirdPartyCore.Model.TpBetRecordExtend
{
    public class BetRecordInfo
    {
        public int TpId { get; set; }
        public string TransId { get; set; }
        public int GameType { get; set; }
        public string TpUserName { get; set; }
        public long SettledTime { get; set; }
        public long TransTime { get; set; }
        public decimal Stake { get; set; }
        public decimal? ValidBet { get; set; }
        public decimal? Revenue { get; set; }
        public decimal ProgressiveBet { get; set; }
        public decimal ProgressiveWin { get; set; }
        public decimal WinlostAmount { get; set; }
        public int SettlementType { get; set; }
        public int TicketStatus { get; set; }
        public string OriginalState { get; set; }
        public int TicketKey { get; set; }
        public int BonusId { get; set; }
        public string ReferenceNo { get; set; }
    }
}
