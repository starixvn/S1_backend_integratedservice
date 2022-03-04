using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerKMService
    {
        private readonly MySqlClient _sqlClient;

        public WagerKMService(IServiceProvider provider)
        {
            _sqlClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public async Task<int> InsertWager(WagerKMModel bet)
        {
            return await _sqlClient.ExecuteAsync("Service_TpWagerKM_Ins", new DynamicParameters(bet));
        }
    }
}