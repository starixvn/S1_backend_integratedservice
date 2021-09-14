using System.Linq;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class BetLimitService
    {
        private int _minBet;
        private int _maxBet;

        public BetLimitService SetBetLimit(int tpCategory, int tpId, CustBetLimit betLimit)
        {
            var category = betLimit.BetSettings?.FirstOrDefault(a => a.TpCategory == tpCategory);
            if(category == null)
                return this;

            var tp = category.TpBetLimits?.FirstOrDefault(a => a.TpId == tpId);
            if (tp != null)
            {
                _minBet = tp.MinBet;
                _maxBet = tp.MaxBet;
                return this;
            }
            _minBet = category.MinBet;
            _maxBet = category.MaxBet;
            return this;
        }

        public bool IsOutOfLimit(decimal betAmount)
        {
            if (_minBet != 0 && _maxBet != 0)
            {
                return betAmount < _minBet || betAmount > _maxBet;
            }
            return false;
        }
    }
}