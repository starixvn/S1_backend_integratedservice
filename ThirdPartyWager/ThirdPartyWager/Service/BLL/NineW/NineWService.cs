using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class NineWService
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<NineWService> _logger;
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly MySqlClient _dbClient;
        private readonly int tpId = (int)Tp.NineW;

        public NineWService(IServiceProvider provider, IConfiguration configuration)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _logger = provider.GetRequiredService<ILogger<NineWService>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();

            var log = provider.GetRequiredService<ILogger<MySqlClient>>();
            var conn = configuration.GetConnectionString("ConnectionStringWager");
            _dbClient = new MySqlClient(conn, log);
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            try
            {
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);
                var result = await QuerySettleBetHistoryAsync(new NineWConfig(setting), startTime, endTime);

                var tasks = result.Select(item => Task.Run(() => InsertWager(item))).ToArray();
                await Task.WhenAll(tasks);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NineW GetWagersAsync Exception.");
                return false;
            }

        }

        private async Task<List<SettleBet>> QuerySettleBetHistoryAsync(NineWConfig config, string startTime, string endTime)
        {
            var page = 1;
            var totalPage = 1;
            var result = new List<SettleBet>();

            while (page <= totalPage)
            {
                var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "cert", config.Cert },
                    { "startDate", startTime },
                    { "endDate", endTime },
                    { "timeZone", "1"},
                    { "pageNumber", page.ToString() },
                });
                var response = await _httpHelper.GetClient().PostAsync(config.WagerUrl, httpContent);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(response.ReasonPhrase);
                }

                var json = await response.Content.ReadAsStringAsync();
                var queryReuslt = _common.FromJson<QuerySettleBetHistoryObj>(json);
                if (queryReuslt.status != "1")
                {
                    throw new Exception($"NineW querySettleBetHistory fail. status is {queryReuslt.status}.");
                }
                result.AddRange(queryReuslt.SettleBetList.Where(d => d.agentId == config.Agent));
                page++;
                totalPage = queryReuslt.totalPage;
            }

            return result;
        }


        public Task<int> InsertWager(SettleBet bet)
        {
            return _dbClient.ExecuteAsync("Service_TpWager9W_Ins", new DynamicParameters(bet));
        }
    }
}

