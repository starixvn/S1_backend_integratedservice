namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_AMB
    {
        
    }
    
    public class SettleDataRNG_AMB
    {
        public string TransId { get; set; }
        public string TpUserName { get; set; }
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal ProgressiveBet { get; set; }
        public decimal ProgressiveWin { get; set; }
        public decimal WinLostAmount { get; set; }
        public long SettledTime { get; set; }
        public long TransTime { get; set; }
        public string GameId { get; set; }
    }
}