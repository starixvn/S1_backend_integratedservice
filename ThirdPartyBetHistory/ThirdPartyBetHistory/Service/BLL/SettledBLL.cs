using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using ThirdPartyBetHistory.Service.DAL;
using ThirdPartyBetHistory.Service.Model;

namespace ThirdPartyBetHistory.Service.BLL
{
    public class SettledBLL
    {
        private readonly SettledService _settledService;

        public SettledBLL(IServiceProvider provider)
        {
            _settledService = provider.GetRequiredService<SettledService>();
        }
        public IEnumerable<BonusFulfill> GetBonusFulfill()
        {
            return _settledService.GetBonusFulfill();
        }
        public bool BonusSettle(BonusFulfill bonusFulfill)
        {
            return _settledService.BonusSettle(bonusFulfill);
        }
    }
}
