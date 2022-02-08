using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoKiss918BLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiKiss918BLL _apiKiss918BLL;
        private readonly WagerKiss918Service _wagerKiss918Service;
        private readonly ILogger<WagerRedoKiss918BLL> _logger;

        private readonly int _tpId = (int)Tp.Kiss918;

        public WagerRedoKiss918BLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiKiss918BLL = provider.GetRequiredService<ApiKiss918BLL>();
            _wagerKiss918Service = provider.GetRequiredService<WagerKiss918Service>();
            _logger = provider.GetRequiredService<ILogger<WagerRedoKiss918BLL>>();
        }

        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix, string currency)
        {
            string msg = "FATAL_ERROR";
            string guid = Guid.NewGuid().ToString();
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                DateTime getReportStartTime = DateTime.Parse(startTime);
                string ReportStartTime = getReportStartTime.ToString("yyyy-MM-dd");
                DateTime getReportEndTime = DateTime.Parse(endTime);
                string ReportEndTime = getReportEndTime.ToString("yyyy-MM-dd");

                // get report player
                var dataReportPlayer = await _apiKiss918BLL.GetReportPlayerListByTime(setting["WagerUrl"], setting[$"authcode_{currency}"], setting[$"secretkey_{currency}"], setting[$"userName_{currency}"], ReportStartTime, ReportEndTime, guid);

                if (!dataReportPlayer.success)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataReportPlayer)}");
                    return false;
                }

                if (dataReportPlayer.success && dataReportPlayer.results.Count() == 0)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataReportPlayer)}");
                    return true;
                }

                //count interval wagers
                var getWagerCount = _wagerKiss918Service.GetWagerTotal(startUnix, endUnix, currency);
                Dictionary<string, int> keyValuePairs = new Dictionary<string, int>();
                foreach (var wagerItem in getWagerCount)
                {
                    keyValuePairs.Add(wagerItem.Account, wagerItem.wagerCount);
                }

                int perPage = 100;
                foreach (var reportList in dataReportPlayer.results)
                {
                    int page = 1;
                    int totalPage = 1;
                    var Account = reportList.Account;
                    while (page <= totalPage)
                    {
                        // get wager
                        var dataWager = await _apiKiss918BLL.GetBetListByTime(setting["WagerUrl"], setting[$"authcode_{currency}"], setting[$"secretkey_{currency}"], setting[$"userName_{currency}"], Account, startTime, endTime, page, perPage, guid);

                        // error condition
                        var errorCode = dataWager.code;
                        if (errorCode == 0 && dataWager.total == 0)
                        {
                            msg = "NO_DATA";
                            _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                            break;
                        }

                        if (errorCode != 0)
                        {
                            msg = "THIRD_PARTY_ERROR";
                            _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                            return false;
                        }

                        if (keyValuePairs.ContainsKey(Account) != false && dataWager.total == keyValuePairs[Account])
                        {
                            msg = "MATCH";
                            _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                            break;
                        }

                        totalPage = (int)((dataWager.total / perPage) + 1);
                        foreach (var bet in dataWager.results)
                        {
                            try
                            {
                                //if (bet.LogDataType != 2)
                                //{
                                //    continue;
                                //}
                                bet.Currency = currency;
                                bet.Account = Account;
                                _wagerKiss918Service.InsertWager(bet);
                            }
                            catch (Exception ex)
                            {
                                msg = $"INSERT_WAGER(WAGERID: {bet.uuid} )";
                                _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                                return false;
                            }
                        }
                        page++;
                    }
                }

                msg = "FINISH";
                _logger.LogInformation($"RedoWagersAsync {msg}\r\n{startTime} ~ {endTime}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"RedoWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }
    }
}
