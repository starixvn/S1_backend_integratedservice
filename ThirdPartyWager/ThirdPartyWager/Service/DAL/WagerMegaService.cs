using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerMegaService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerMegaService(IServiceProvider provider, IConfiguration configuration)
        {
            var log = provider.GetRequiredService<ILogger<MySqlClient>>();
            _wagerDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionStringWager"), log);
        }

        public Task<int> InsertReport(ReportListMega report)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpReportRng_Mega_Ins", new DynamicParameters(report));
        }
    }
}
