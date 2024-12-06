using ThirdPartyWager.Extensions;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.NineW;

namespace ThirdPartyWager.Service.BLL
{
    public class NineWBLL
    {
        private readonly ILogger<NineWBLL> _logger;
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly MySqlClient _wagerDbClient;
        private readonly int _tpId = (int)Tp.NineW;
        private readonly string _tpName = Tp.NineW.ToString();
        private readonly NineWConfig _siteConfig;
        private readonly FinanceService _fincanceService;
        private readonly IEnumerable<TpCurrency> _currencyList;
        private readonly int _timeZone = 8;
        private readonly List<int> _reportType = new() { 0, 2, 3, 7 };
        private string _domain;
        private readonly Log2File _log2File;

        public NineWBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<NineWBLL>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _fincanceService = provider.GetRequiredService<FinanceService>();
            _currencyList = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList(_tpId);
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<NineWConfig>(Tp.NineW.ToString());
            _log2File = provider.GetRequiredService<Log2File>();
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            try
            {
                var result = await QuerySettleBetHistoryAsync(_siteConfig, startTime, endTime);
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

        public async Task<bool> GetUnsettleWagersAsync(long startTime)
        {
            try
            {
                var result = await QueryUnSettleSummaryTransactionAsync(_siteConfig, startTime);
                var tasks = result.Select(item => Task.Run(() => InsertWager(item))).ToArray();
                await Task.WhenAll(tasks);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NineW GetUnsettleWagersAsync Exception.");
                return false;
            }
        }

        public async Task<bool> GetReSettleWagersAsync(string startTime, string endTime)
        {
            try
            {
                var result = await QueryReSettleSummaryTransactionAsync(_siteConfig, startTime, endTime);
                var tasks = result.Select(item => Task.Run(() => InsertWager(item))).ToArray();

                // update resettledDate to TpReport9W
                foreach (var row in result)
                {
                    if (row.resettledDate > new DateTime(1970, 1, 1))
                    {
                        _ = await UpdateReport(row);
                        //_logger.LogError($"TpReport9W update userId: {row.userId}, eventId: {row.eventId}, marketId: {row.marketId}, resettledDate: {row.resettledDate}");
                    }
                }

                await Task.WhenAll(tasks);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NineW GetReSettleWagersAsync Exception.");
                return false;
            }
        }

        public async Task<bool> GetCountSummaryTransactionReportAsync(DateTime date)
        {
            try
            {
                var dailySummary = await CountSummaryTransactionByHourAsync(_siteConfig, date);
                if (!dailySummary.Any())
                {
                    return true;
                }
                var result = _fincanceService.SetFinanceTpDailySummary(dailySummary);
                return result == dailySummary.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"NineW CountSummaryTransactionByHourAsync Exception. ({ex.Message})");
                throw;
            }
        }

        public async Task<bool> GetSummaryTransactionReportAsync(DateTime date)
        {
            try
            {
                var summaryTransaction = await QuerySummaryTransactionAsync(_siteConfig, date);
                var tasks = summaryTransaction.Select(item => Task.Run(() => InsertReport(item))).ToArray();
                await Task.WhenAll(tasks);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"NineW GetSummaryTransactionReportAsync Exception. ({ex.Message})");
                throw;
            }

        }

        private async Task<List<SettleBet>> QuerySettleBetHistoryAsync(NineWConfig config, string startTime, string endTime)
        {
            var result = new List<SettleBet>();
            var agents = GetAgents();
            var wagerUrl = $"{_domain}/{config.WebSite}/querySettleBetHistory";

            foreach (var type in _reportType)
            {
                var page = 1;
                var totalPage = 1;
                while (page <= totalPage)
                {
                    await Task.Delay(1500);
                    var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "cert", config.Cert },
                        { "startDate", startTime },
                        { "endDate", endTime },
                        { "timeZone", "1"},
                        { "pageNumber", page.ToString() },
                        { "reportType", type.ToString() }
                    });
                    var pReq = httpContent.ReadAsStringAsync().Result;
                    _log2File.InsertRequestLog(_tpName, wagerUrl, pReq, "1");

                    var response = await _httpHelper.GetClient().PostAsync(wagerUrl, httpContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        _log2File.InsertResponseLog(_tpName, wagerUrl, pReq, response.ReasonPhrase);
                        throw new Exception($"StatusCode:{response.StatusCode} | Reason:{response.ReasonPhrase}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation(json);
                    var queryReuslt = _common.FromJson<QuerySettleBetHistoryObj<SettleBet>>(json);
                    if (queryReuslt.status != "1")
                    {
                        throw new Exception($"NineW querySettleBetHistory fail. status is {queryReuslt.status}.");
                    }
                    result.AddRange(queryReuslt.BetList.Where(d => agents.Contains(d.agentId)));
                    page++;
                    totalPage = queryReuslt.totalPage;
                }
            }
            return result;
        }

        private async Task<List<SettleBet>> QueryReSettleSummaryTransactionAsync(NineWConfig config, string startTime, string endTime)
        {
            var result = new List<SettleBet>();
            var apiUrl = $"{_domain}/{config.WebSite}/queryReSettleSummaryTransaction";
            var agents = GetAgents();

            foreach (var type in _reportType)
            {
                var page = 1;
                var totalPage = 1;
                while (page <= totalPage)
                {
                    // Frequency: 1 time per minute (by userId + reportType + pageNumber)
                    await Task.Delay(65000);
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
                    var pReq = httpContent.ReadAsStringAsync().Result;
                    _log2File.InsertRequestLog(_tpName, apiUrl, pReq, "1");

                    var response = await _httpHelper.GetClient().PostAsync(apiUrl, httpContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        _log2File.InsertResponseLog(_tpName, apiUrl, pReq, response.ReasonPhrase);
                        throw new Exception($"StatusCode:{response.StatusCode} | Reason:{response.ReasonPhrase}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Url={apiUrl}, Input={httpContent.ReadAsStringAsync().Result}, Output={json}");

                    var queryResult = _common.FromJson<QuerySettleBetHistoryObj<ReSettleBet>>(json);
                    if (queryResult.status != "1")
                    {
                        throw new Exception($"NineW querySettleBetHistory fail. status is {queryResult.status}.");
                    }

                    var betList = queryResult.BetList.Where(d => agents.Contains(d.agentId))?.Select(resettle => new SettleBet(resettle));
                    result.AddRange(betList);
                    page++;
                    totalPage = queryResult.totalPage;
                }
            }
            return result;
        }

        private async Task<List<SettleBet>> QueryUnSettleSummaryTransactionAsync(NineWConfig config, long startTime)
        {
            var result = new List<SettleBet>();
            var apiUrl = $"{_domain}/{config.WebSite}/queryCurrentBetsByLastUpdateDate";
            var agents = GetAgents();

            foreach (var type in _reportType)
            {
                var page = 1;
                var totalPage = 1;
                while (page <= totalPage)
                {
                    await Task.Delay(1500);
                    var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "cert", config.Cert },
                        { "lastUpdateDate", startTime.ToString() },
                        { "timeZone", "1"},
                        { "isTxnDetail", "1"},
                        { "pageNumber", page.ToString() },
                        { "reportType", type.ToString() }
                    });
                    var pReq = httpContent.ReadAsStringAsync().Result;
                    _log2File.InsertRequestLog(_tpName, apiUrl, pReq, "1");

                    var response = await _httpHelper.GetClient().PostAsync(apiUrl, httpContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        _log2File.InsertResponseLog(_tpName, apiUrl, pReq, response.ReasonPhrase);
                        throw new Exception($"StatusCode:{response.StatusCode} | Reason:{response.ReasonPhrase}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation(json);
                    var queryResult = _common.FromJson<UnSettleBet>(json);
                    if (queryResult.status != "1")
                    {
                        throw new Exception($"NineW queryCurrentBetsByLastUpdateDate fail. status is {queryResult.status}.");
                    }

                    var matchedList = queryResult.matched.resultList.Where(d => agents.Contains(d.agentId))?.Select(resettle => new SettleBet(resettle));
                    result.AddRange(matchedList);

                    var unmatchedList = queryResult.unmatched.resultList.Where(d => agents.Contains(d.agentId))?.Select(resettle => new SettleBet(resettle));
                    result.AddRange(unmatchedList);

                    page++;
                    totalPage = queryResult.totalPage;
                }
            }
            return result;
        }

        private async Task<List<FincanceDailySummary>> CountSummaryTransactionByHourAsync(NineWConfig config, DateTime date)
        {
            var result = new List<FincanceDailySummary>();
            var apiUrl = $"{_domain}/{config.WebSite}/countSummaryTransactionByHour";
            if (string.IsNullOrEmpty(_domain) || _domain == "")
            {
                throw new Exception($"_domain is {_domain}");
            }

            foreach (var currency in _currencyList)
            {
                var checkDataList = new List<Checkdata>();
                foreach (var type in _reportType)
                {
                    foreach (var range in date.DateToTimeRange(_timeZone, 12))
                    {
                        await Task.Delay(1500);

                        var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            { "cert", config.Cert },
                            { "currency", currency.TpCurrencyId },
                            { "settleDateStart", range.begin.ToString("yyyy-MM-dd HH:mm") },
                            { "settleDateEnd", range.end.ToString("yyyy-MM-dd HH:mm") },
                            { "timeZone", "1"},
                            { "reportType", type.ToString() }
                        });
                        var pReq = httpContent.ReadAsStringAsync().Result;
                        _log2File.InsertRequestLog(_tpName, apiUrl, pReq, "1");

                        var response = await _httpHelper.GetClient().PostAsync(apiUrl, httpContent);
                        if (!response.IsSuccessStatusCode)
                        {
                            _log2File.InsertResponseLog(_tpName, apiUrl, pReq, response.ReasonPhrase);
                            throw new Exception($"StatusCode:{response.StatusCode} | Reason:{response.ReasonPhrase}");
                        }

                        var json = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation(json);
                        var queryResult = _common.FromJson<CountSummaryTransaction>(json);
                        if (queryResult.status == "1014")//no data.
                        {
                            continue;
                        }

                        if (queryResult.status != "1")
                        {
                            throw new Exception($"NineW CountSummaryTransactionByHourAsync fail. status is {queryResult.status}.");
                        }

                        checkDataList.AddRange(queryResult.checkData);
                    }
                }

                var stake = checkDataList.Sum(i => i.stakeTotal);
                var winLostAmount = checkDataList.Sum(i => i.profitLoss);
                var revenue = checkDataList.Sum(i => i.commission);

                if (stake > 0 || winLostAmount > 0 || revenue > 0)
                {
                    result.Add(new FincanceDailySummary()
                    {
                        SummaryType = SummaryType.TpBetRecord,
                        SummaryDate = date,
                        TpId = _tpId,
                        CurrencyId = currency.CurrencyId,
                        Stake = stake,
                        ValidBet = stake,
                        WinlostAmount = winLostAmount,
                        Revenue = revenue,
                    });
                }
            }
            return result;
        }

        private async Task<List<SummaryTransaction>> QuerySummaryTransactionAsync(NineWConfig config, DateTime date)
        {
            var result = new List<SummaryTransaction>();
            var range = date.DateToTimeRange(_timeZone);
            var apiUrl = $"{_domain}/{config.WebSite}/querySummaryTransaction";
            if (string.IsNullOrEmpty(_domain) || _domain == "")
            {
                throw new Exception($"_domain is {_domain}");
            }

            var agents = GetAgents();

            foreach (var type in _reportType)
            {
                var page = 1;
                var totalPage = 1;
                while (page <= totalPage)
                {
                    await Task.Delay(1500);
                    var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "cert", config.Cert },
                        { "startDate", range.begin.ToString("yyyy-MM-dd HH:mm") },
                        { "endDate", range.end.ToString("yyyy-MM-dd HH:mm") },
                        { "timeZone", "1"},
                        { "pageNumber", page.ToString() },
                        { "reportType", type.ToString() }
                    });
                    var pReq = httpContent.ReadAsStringAsync().Result;
                    _log2File.InsertRequestLog(_tpName, apiUrl, pReq, "1");

                    var response = await _httpHelper.GetClient().PostAsync(apiUrl, httpContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        _log2File.InsertResponseLog(_tpName, apiUrl, pReq, response.ReasonPhrase);
                        throw new Exception($"StatusCode:{response.StatusCode} | Reason:{response.ReasonPhrase}");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation(json);
                    var queryResult = _common.FromJson<QuerySettleBetHistoryObj<SummaryTransaction>>(json);


                    if (queryResult.status == "1014")//no data.
                    {
                        continue;
                    }

                    if (queryResult.status != "1")
                    {
                        throw new Exception($"NineW QuerySummaryTransaction fail. status is {queryResult.status}.");
                    }
                    result.AddRange(queryResult.BetList.Where(d => agents.Contains(d.agentId)));
                    page++;
                    totalPage = queryResult.totalPage;
                }
            }
            return result;
        }

        private Task<int> InsertWager(SettleBet bet)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWager9W_Ins", bet);
        }

        private Task<int> UpdateReport(SettleBet bet)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpReport9W_Set", bet);
        }

        private Task<int> InsertReport(SummaryTransaction data)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpReport9W_Ins", data);
        }

        private List<string> GetAgents()
        {
            var data = new List<string>();
            foreach (var rows in _siteConfig.CurrencyData)
            {
                data.Add(rows.Value.Agent);
            }
            return data;
        }

        public async Task QueryDomain()
        {
            var apiUrl = $"{_siteConfig.DomainUrl}/{_siteConfig.WebSite}/queryDomain";
            var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "cert", _siteConfig.Cert }
            });

            var response = await _httpHelper.GetClient().PostAsync(apiUrl, httpContent);
            var json = response.Content.ReadAsStringAsync().Result;
            var result = _common.FromJson<QueryDomainResponse>(json);

            if (result.status == "1")
            {
                var url = $"{_siteConfig.ApiUrl}.{result.Domains?.FirstOrDefault(x => x.priority == 1)?.domain}/api/apiWallet";
                _domain = url;
            }
            else
            {
                throw new Exception($"9w Query Domain error status is : {result.status}");
            }
        }
    }
}

