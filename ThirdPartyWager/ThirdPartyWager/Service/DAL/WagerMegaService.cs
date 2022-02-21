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

        public IEnumerable<SettleDataMega> GetReportPlayerUnWagerList()
        {
            return _wagerDbClient.Query<SettleDataMega>("Service_TpReportMega_UnWager_Get");
        }

        public Task<int> InsertWager(BetMega bet)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWagerMega_Ins", new DynamicParameters(bet));
        }

        public Task<int> SetAddSecond(string loginId, long StartAt, int AddSecond)
        {
            var p = new DynamicParameters(new
            {
                loginId = loginId,
                StartAt = StartAt,
                AddSecond = AddSecond
            });
            return _wagerDbClient.ExecuteAsync("Service_TpReportMega_AddSecond_Set", p);
        }

        public IEnumerable<SettleDataMega> GetReportPlayerUnCheckList()
        {
            return _wagerDbClient.Query<SettleDataMega>("Service_TpReportMega_UnCheck_Get");
        }

        public SummaryMega GetWagerPlayerSummary(string loginId, long beginTime, long endTime)
        {
            var p = new DynamicParameters(new
            {
                LoginId = loginId,
                BeginTime = beginTime,
                EndTime = endTime
            });
            return _wagerDbClient.QueryFirst<SummaryMega>("Service_TpWagerMegaSummaryByLoginId_Get", p);
        }

        public Task<int> SetCheckFlag(string loginId, long startAt, int checkFlag)
        {
            var p = new DynamicParameters(new
            {
                loginId = loginId,
                StartAt = startAt,
                CheckFlag = checkFlag
            });
            return _wagerDbClient.ExecuteAsync("Service_TpReportMega_Check_Set", p);
        }
    }
}
