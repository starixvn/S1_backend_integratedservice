using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerMegaService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerMegaService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public Task<int> InsertReport(ReportListMega report)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpReportRng_Mega_Ins", new DynamicParameters(report));
        }
    }
}
