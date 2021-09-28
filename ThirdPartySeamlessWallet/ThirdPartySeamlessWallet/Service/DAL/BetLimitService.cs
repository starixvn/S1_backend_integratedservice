using System.Linq;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class BetLimitService
    {
        public int MinBet { get; set; }
        public int MaxBet { get; set; }

        public BetLimitService SetBetLimit(int tpCategory, int gameTypeId, CustBetLimit betLimit)
        {
            var category = betLimit.BetSettings?.FirstOrDefault(a => a.TpCategory == tpCategory);
            if(category == null)
                return this;

            var gameType = category.TpBetSetting?.FirstOrDefault(a => a.GameTypeId == gameTypeId);
            if (gameType != null)
            {
                MinBet = gameType.MinBet;
                MaxBet = gameType.MaxBet;
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