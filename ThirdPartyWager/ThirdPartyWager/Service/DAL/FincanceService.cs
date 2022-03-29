using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class FincanceService
    {
        private readonly MySqlClient _mainDbClient;


        public FincanceService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);

        }

        public int SetFinanceTpDailySummary(IEnumerable<FincanceDailySummary> dailySummary)
        {
           return _mainDbClient.Execute("Finance_Tp_DailySummary_Set", dailySummary);
        }
    }
}
