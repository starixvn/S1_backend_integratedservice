using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class RewardAllBetBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiAllBetBLL _apiAllBetBLL;
        private readonly CommonBLL _commonBLL;
        private readonly WagerAllBetService _wagerAllBetService;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ILogger<RewardAllBetBLL> _logger;

        private readonly int _tpId = (int)Tp.Allbet;
        private readonly string _tpName = Tp.Allbet.ToString();
        private readonly int _timeZone = 8;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        private IEnumerable<TpCurrency> _currencyList;
        private readonly IWebHostEnvironment _env;
        private readonly Dictionary<int, AllBetConfig> _siteConfigDic;

        public RewardAllBetBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiAllBetBLL = provider.GetRequiredService<ApiAllBetBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _wagerAllBetService = provider.GetRequiredService<WagerAllBetService>();
            _logger = provider.GetRequiredService<ILogger<RewardAllBetBLL>>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _currencyList = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList(_tpId);
            _env = provider.GetRequiredService<IWebHostEnvironment>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<AllBetConfig>(Tp.Allbet.ToString());
        }

        public async Task<bool> ExecRewardWorker(int type, int timeRange)
        {
            var wokerTime = GetWorkerTime(type, 0);
            var quick = CheckQuick(wokerTime, 5);
            wokerTime = wokerTime.AddMinutes(-3);
            var time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                var result = await GetRewardsAsync(time.startTime, time.endTime);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimer(_tpId, type, time.endUnix);
                }
            }

            return quick;
        }

        private async Task<bool> GetRewardsAsync(string startTime, string endTime)
        {
            var msg = "FATAL_ERROR";

            try
            {
                var success = true;
                if (_env.IsDevelopment())
                {
                    _currencyList = _currencyList.Take(1);
                }
                var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId);
                var serverList = _commonBLL.GetSiteList();

                foreach (var site in siteList)
                {
                    if (site.TicketKey == 0)
                    {
                        continue;
                    }

                    var _siteAllBetConfig = _siteConfigDic[site.SiteId];
                    var currencyList = JsonConvert.DeserializeObject<IEnumerable<int>>(serverList.First(x => x.SiteId == site.SiteId).CurrencyList);

                    foreach (var currency in _currencyList.Where(x => currencyList.Contains(x.CurrencyId)))
                    {
                        try
                        {
                            // pagination
                            var pageIndex = 1;
                            var pageRowCnt = 1;
                            var agent = _siteAllBetConfig.CurrencyAgents.FirstOrDefault(x => x.CurrencyId == currency.TpCurrencyId)?.Agent;

                            while (pageRowCnt >= 1)
                            {
                                // get reward
                                var rewardData = _apiAllBetBLL.GetRewardByTime(agent, startTime, endTime, pageIndex, site.SiteId);

                                // error condition
                                if (rewardData == null)
                                {
                                    msg = "THIRD_PARTY_ERROR";
                                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(rewardData)}");
                                    return false;
                                }

                                if (rewardData.resultCode != "OK")
                                {
                                    msg = "THIRD_PARTY_ERROR";
                                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(rewardData)}");
                                    return false;
                                }

                                pageRowCnt = rewardData.data.list.Count();
                                if (pageRowCnt == 0)
                                {
                                    msg = "NO_DATA";
                                    _logger.LogInformation($"GetRewardsAsync {msg}\r\n{JsonConvert.SerializeObject(rewardData)}");
                                    break;
                                }

                                // check reward data
                                foreach (var item in rewardData.data.list)
                                {
                                    try
                                    {
                                        var reward = _wagerAllBetService.GetRewardRecord(item.eventRecordNum).Result;
                                        if (item.status != 3)
                                        {
                                            //活动记录状态 2: 未结算(Not settled) 3: 已结算(Settled)
                                            _logger.LogError($"Reward Not Settled: {item.eventRecordNum} {item.settleTime}");
                                            continue;
                                        }
                                        if (reward == null)
                                        {
                                            // call sw reward api
                                            var response = _apiAllBetBLL.CallEventSettleApi(item).Result;
                                            var result = JsonConvert.DeserializeObject<AllBetTransferResponse>(response);
                                            if (result.resultCode != 0)
                                            {
                                                if (result.resultCode != 10007)
                                                {
                                                    success = false;
                                                    msg = "SEAMLESS_ERROR";
                                                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(result)}");
                                                }
                                            }
                                            else
                                            {
                                                LogContext.PushProperty("SystemJob", $"Auto-Dispatch {_tpName}");
                                                _logger.LogInformation($"System Reward Settled: {item.eventRecordNum}");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        msg = $"INSERT_WAGER(WAGERID: {item.eventRecordNum} )";
                                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(item)}\r\n{ex.Message}");
                                        return false;
                                    }
                                }
                                if (!success)
                                {
                                    return false;
                                }
                                pageIndex++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"{currency.TpCurrencyId}Agent GetRewardsAsync error. \r\n{ex.Message}");
                        }
                    }
                }

                msg = "FINISH";
                _logger.LogInformation($"GetRewardsAsync {msg}\r\n{startTime} ~ {endTime}");
                return success;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetRewardsAsync\r\n{ex.Message}");
                return false;
            }
        }


        private DateTime GetWorkerTime(int type, int vary)
        {
            var lastTime = _thirdPartyBLL.GetThirdPartyTimer(_tpId, type);
            return (lastTime == null) ? DateTime.Now.AddHours(-2) : lastTime.StartTime.AddSeconds(vary);
        }

        private bool CheckQuick(DateTime workTime, int excess)
        {
            var diff = DateTime.Now - workTime;
            return DateTime.Now > workTime && diff.TotalMinutes > excess;
        }
    }
}
