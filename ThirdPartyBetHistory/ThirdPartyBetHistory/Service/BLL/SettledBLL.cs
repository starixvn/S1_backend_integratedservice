using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyBetHistory.Service.DAL;
using ThirdPartyBetHistory.Service.Model;

namespace ThirdPartyBetHistory.Service.BLL
{
    public class SettledBLL
    {
        private readonly SettledService settledService;

        public SettledBLL(SettledService settledService)
        {
            this.settledService = settledService;
        }
        public IEnumerable<BonusFulfill> GetBonusFulfill()
        {
            return settledService.GetBonusFulfill();
        }
        public bool BonusSettle(BonusFulfill bonusFulfill)
        {
            return settledService.BonusSettle(bonusFulfill);
        }
    }
}
