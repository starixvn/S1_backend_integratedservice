using System.Collections.Generic;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class CustBetLimit
    {
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public List<BetSetting> BetSettings { get; set; }
    }

    public class BetSetting
    {
        public int TpCategory { get; set; }
        public int MinBet { get; set; }
        public int MaxBet { get; set; }
        public List<TpBetLimit> TpBetLimits { get; set; }
    }

    public class TpBetLimit
    {
        public int TpId { get; set; }
        public int MinBet { get; set; }
        public int MaxBet { get; set; }
    }
}