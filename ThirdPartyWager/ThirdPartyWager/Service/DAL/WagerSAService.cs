using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerSAService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerSAService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }
        public int InsertWager(IEnumerable<BetSA> bets, IDbTransaction transaction)
        {
            return _wagerDbClient.Execute("Service_TpWagerSA_Ins",
                transaction,
                bets);
        }

        public WagerSATotal GetWagerTotal(long beginTime, long endTime)
        {
            return _wagerDbClient.QueryFirst<WagerSATotal>("Service_TpWagerSATotal_Get", new { BeginTime = beginTime, EndTime = endTime });
        }
    }
}
