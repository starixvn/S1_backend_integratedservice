using System.Linq;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class BetLimitService
    {
        public int MinBet { get; set; }
        public int MaxBet { get; set; }

        public BetLimitService SetBetLimit(int tpCategory, int tpId, CustBetLimit betLimit)
        {
            var category = betLimit.BetSettings?.FirstOrDefault(a => a.TpCategory == tpCategory);
            if(category == null)
                return this;

            var tp = category.TpBetLimits?.FirstOrDefault(a => a.TpId == tpId);
            if (tp != null)
            {
                MinBet = tp.MinBet;
                MaxBet = tp.MaxBet;
                return this;
            }
            MinBet = category.MinBet;
            MaxBet = category.MaxBet;
            return this;
        }

        public bool IsOutOfLimit(decimal betAmount)
        {
            if (MinBet != 0 && MaxBet != 0)
            {
                return betAmount < MinBet || betAmount > MaxBet;
            }
            return false;
        }
    }
}