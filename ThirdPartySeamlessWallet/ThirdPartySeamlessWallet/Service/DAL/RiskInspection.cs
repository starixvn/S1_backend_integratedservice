using System.Collections.Generic;
using System.Linq;
using ThirdPartySeamlessWallet.Service.DAL.Interface;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class RiskInspection
    {
        private readonly ICustBetLimitService _custBetLimitService;
        public RiskInspection(ICustBetLimitService custBetLimitService)
        {
            _custBetLimitService = custBetLimitService;
        }
        
        public bool IsOutOfLimit(int custId, int tpCategory, int gameTypeId, decimal amount, out int minBet, out int maxBet)
        {
            minBet = 0;
            maxBet = 0;
            var custBetLimit = _custBetLimitService.GetCustBetLimit(custId);
            if(custBetLimit == null)
                return false;

            var betSetting = new BetLimit(custBetLimit.BetSettings)
                .SetBetLimit(tpCategory, gameTypeId);

            minBet = betSetting.MinBet;
            maxBet = betSetting.MaxBet;
            if (minBet != 0 && maxBet != 0)
            {
                return amount < minBet || amount > maxBet;
            }
            return false;
        }
        
        public class BetLimit
        {
            public int MinBet { get; set; }
            public int MaxBet { get; set; }
            private readonly List<BetSetting> _betSettings;
            public BetLimit(List<BetSetting> betSettings)
            {
                _betSettings = betSettings;
            }

            public BetLimit SetBetLimit(int tpCategory, int gameTypeId)
            {
                var category = _betSettings?.FirstOrDefault(a => a.TpCategory == tpCategory);
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
        }
    }
}