using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Linq;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerIconicService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerIconicService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public bool InsertWager(BetIconic bet)
        {
            return _wagerDbClient.Execute("Service_TpWagerFish_Iconic_Ins", new DynamicParameters(bet));
        }

        public bool InsertWagerFish(BetData_AT bet)
        {
            return _wagerDbClient.Execute("Service_TpWagerFish_AT_Ins", new DynamicParameters(bet));
        }

        public bool InsertWagerSlot(BetData_AT bet)
        {
            return _wagerDbClient.Execute("Service_TpWagerRng_AT_Ins", new DynamicParameters(bet));
        }

        public TpWagerSummary_AT GetWagerFishTotal(long beginTime, long endTime, string currency)
        {
            return _wagerDbClient.QueryFirst<TpWagerSummary_AT>("Service_TpWagerFish_AT_Total_Get", new DynamicParameters(new { BeginTime = beginTime, EndTime = endTime, Currency = currency }));
        }
        public TpWagerSummary_AT GetWagerSlotTotal(long beginTime, long endTime, string currency)
        {
            return _wagerDbClient.QueryFirst<TpWagerSummary_AT>("Service_TpWagerRng_AT_Total_Get", new DynamicParameters(new { BeginTime = beginTime, EndTime = endTime, Currency = currency }));
        }
    }
}
