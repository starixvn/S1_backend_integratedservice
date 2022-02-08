using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerMPService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerMPService(IServiceProvider provider, IConfiguration configuration)
        {
            var log = provider.GetRequiredService<ILogger<MySqlClient>>();
            var conn = configuration.GetConnectionString("ConnectionStringWager");
            _wagerDbClient = new MySqlClient(conn, log);
        }

        public Task<int> InsertWager(Bet_MP bet)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWagerPK_MP_Ins", new DynamicParameters(bet));
        }

        public WagerMPTotal GetWagerTotal(long beginTime, long endTime)
        {
            return _wagerDbClient.QueryFirst<WagerMPTotal>("Service_TpWagerPK_MP_Total_Get", new DynamicParameters(new { BeginTime = beginTime, EndTime = endTime }));
        }
    }
}
