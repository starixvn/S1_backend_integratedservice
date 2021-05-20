using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ThirdPartyWager.Service.BLL.KM;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerKMService
    {
        private readonly MySqlClient _sqlClient;
        private readonly ILogger<WagerKMService> _logger;
        public WagerKMService(IServiceProvider provider, IConfiguration configuration)
        {
            _logger = provider.GetRequiredService<ILogger<WagerKMService>>();
            var log = provider.GetRequiredService<ILogger<MySqlClient>>();
            var conn = configuration.GetConnectionString("ConnectionStringWager");
            _sqlClient = new MySqlClient(conn, log);
        }
        public async Task<int> InsertWager(WagerKMModel bet)
        {
            return await _sqlClient.ExecuteAsync("Service_TpWagerKM_Ins", new DynamicParameters(bet));
        }
    }
}