using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class NineWBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<NineWBLL> _logger;
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly MySqlClient _wagerDbClient;
        private readonly int _tpId = (int)Tp.NineW;
        private readonly NineWConfig _config;

        public NineWBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _logger = provider.GetRequiredService<ILogger<NineWBLL>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();

            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);

            var setting = _thirdPartyBLL.GetThirdPartySetting(_tpId);
            _config = new NineWConfig(setting);
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            try
            {
                var result = await QuerySettleBetHistoryAsync(_config, startTime, endTime);
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

        public async Task<bool> GetReSettleWagersAsync(string startTime, string endTime)
        {
            try
            {
                var result = await QueryReSettleSummaryTransactionAsync(_config, startTime, endTime);
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
            var result = new List<SettleBet>();
            var reportType = new List<int>() { 0, 2, 3, 7 };
            foreach (var type in reportType)
            {           
                var page = 1;
                var totalPage = 1;
                while (page <= totalPage)
                {
                    _ = Task.Delay(1000);
                    var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "cert", config.Cert },
                        { "startDate", startTime },
                        { "endDate", endTime },
                        { "timeZone", "1"},
                        { "pageNumber", page.ToString() },
                        { "reportType", type.ToString() }
                    });
                    var response = await _httpHelper.GetClient().PostAsync(config.WagerUrl, httpContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception(response.ReasonPhrase);
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation(json);
                    var queryReuslt = _common.FromJson<QuerySettleBetHistoryObj<SettleBet>>(json);
                    if (queryReuslt.status != "1")
                    {
                        throw new Exception($"NineW querySettleBetHistory fail. status is {queryReuslt.status}.");
                    }
                    result.AddRange(queryReuslt.BetList.Where(d => d.agentId == config.Agent));
                    page++;
                    totalPage = queryReuslt.totalPage;
                }
            }
            return result;
        }

        private async Task<List<SettleBet>> QueryReSettleSummaryTransactionAsync(NineWConfig config, string startTime, string endTime)
        {
            var result = new List<SettleBet>();
            var apiurl = $"{config.ApiUrl}/{config.WebSite}/queryReSettleSummaryTransaction";
            var reportType = new List<int>() { 0, 2, 3, 7 };
            foreach (var type in reportType)
            {     
                var page = 1;
                var totalPage = 1;
                while (page <= totalPage)
                {
                    _ = Task.Delay(1000);
                    var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "cert", config.Cert },
                        { "startDate", startTime },
                        { "endDate", endTime },
                        { "timeZone", "1"},
                        { "isTxnDetail", "1"},
                        { "pageNumber", page.ToString() },
                        { "reportType", type.ToString() }
                    });
                    var response = await _httpHelper.GetClient().PostAsync(apiurl, httpContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception(response.ReasonPhrase);
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation(json);
                    var queryReuslt = _common.FromJson<QuerySettleBetHistoryObj<ReSettleBet>>(json);
                    if (queryReuslt.status != "1")
                    {
                        throw new Exception($"NineW querySettleBetHistory fail. status is {queryReuslt.status}.");
                    }

                    var betlist = queryReuslt.BetList.Where(d => d.agentId == config.Agent)?.Select(resettle => new SettleBet(resettle));
                    result.AddRange(betlist);
                    page++;
                    totalPage = queryReuslt.totalPage;
                }
            }
            return result;
        }

        private Task<int> InsertWager(SettleBet bet)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWager9W_Ins", new DynamicParameters(bet));
        }
    }
}

