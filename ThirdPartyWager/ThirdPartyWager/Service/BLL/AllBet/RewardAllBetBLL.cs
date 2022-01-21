using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class RewardAllBetBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiAllBetBLL _apiAllBetBLL;
        private readonly WagerAllBetService _wagerAllBetService;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ILogger<RewardAllBetBLL> _logger;

        private readonly int _tpId = (int)Tp.Allbet;
        private readonly string _tpName = Tp.Allbet.ToString();
        private readonly int _timeZone = 8;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";

        public RewardAllBetBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiAllBetBLL = provider.GetRequiredService<ApiAllBetBLL>();
            _wagerAllBetService = provider.GetRequiredService<WagerAllBetService>();
            _logger = provider.GetRequiredService<ILogger<RewardAllBetBLL>>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        public async Task<bool> ExecRewardWorker(int type, int timeRange)
        {
            var wokerTime = GetWorkerTime(type, 0);
            bool quick = CheckQuick(wokerTime, 5);
            wokerTime = wokerTime.AddMinutes(-3);
            var time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, wokerTime);

            var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (time.endUnix < cvTime)
            {
                bool result = await GetRewardsAsync(time.startTime, time.endTime);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimer(_tpId, type, time.endUnix);
                }
            }

            return quick;
        }

        private async Task<bool> GetRewardsAsync(string startTime, string endTime)
        {
            LogContext.PushProperty("Tp", _tpName);
            string msg = "FATAL_ERROR";
            string guid = Guid.NewGuid().ToString();

            try
            {
                var setting = _thirdPartyBLL.GetThirdPartySetting(_tpId);

                // pagination
                int pageIndex = 1;
                int pageRowCnt = 1;
                bool success = true;

                while (pageRowCnt >= 1)
                {
                    // get reward
                    var data = await _apiAllBetBLL.GetRewardByTime(setting["ApiUrl"], setting["DesKey"], setting["MD5Key"], setting["PropertyId"], startTime, endTime, pageIndex, guid);

                    // error condition
                    if (data == null)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(data)}");
                        return false;
                    }

                    var errorCode = data.error_code;
                    if (errorCode != "OK")
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(data)}");
                        return false;
                    }

                    pageRowCnt = data.page.datas.Count();
                    if (pageRowCnt == 0)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"GetRewardsAsync {msg}\r\n{JsonConvert.SerializeObject(data)}");
                        break;
                    }

                    // check reward data
                    foreach (var item in data.page.datas)
                    {
                        try
                        {
                            var reward = _wagerAllBetService.GetRewardRecord(item.eventRecordNum).Result;
                            if(item.status != 3)
                            {
                                //活动记录状态 2: 未结算(Not settled) 3: 已结算(Settled)
                                _logger.LogError($"Reward Not Settled: {item.eventRecordNum} {item.settleTime}");
                                continue;
                            }
                            if (reward == null)
                            {
                                // call sw reward api
                                var response = _apiAllBetBLL.callSeamlessApi("Reward", item, setting["SHA1key"], setting["PropertyId"]).Result;
                                var result = JsonConvert.DeserializeObject<RewardAllBetModel>(response);
                                if (result.error_code != "0")
                                {
                                    if (result.error_code != "10007")
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
                            _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(item)}\r\n{ex.Message}");
                            _logger.LogError(ex, ex.Message);
                            return false;
                        }
                    }
                    if (!success)
                    {
                        return false;
                    }
                    pageIndex++;
                }

                msg = "FINISH";
                _logger.LogInformation($"GetRewardsAsync {msg}\r\n{startTime} ~ {endTime}");
                return success;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError($"GetRewardsAsync\r\n{ex.Message}");
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
