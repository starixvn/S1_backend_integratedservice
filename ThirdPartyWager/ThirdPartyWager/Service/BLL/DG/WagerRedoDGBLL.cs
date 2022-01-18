using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoDGBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiDGBLL _apiDGBLL;
        private readonly WagerDGService _wagerDGService;
        private readonly ILogger<WagerRedoDGBLL> _logger;

        private readonly int _tpId = (int)Tp.DG;
        private readonly string _tpName = Tp.DG.ToString();

        public WagerRedoDGBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiDGBLL = provider.GetRequiredService<ApiDGBLL>();
            _wagerDGService = provider.GetRequiredService<WagerDGService>();
            _logger = provider.GetRequiredService<ILogger<WagerRedoDGBLL>>();
        }

        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix)
        {
            string msg = "FATAL_ERROR";
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", _tpName);
            try
            {
                // get dg api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                // count interval wagers
                var getWagerCount = _wagerDGService.GetWagerDGTotal(startUnix, endUnix);
                int wagerCount = getWagerCount.wagerCount;

                // pagination
                int page = 1;
                int totalPage = 1;
                while (page <= totalPage)
                {
                    var dataWager = await _apiDGBLL.GetBetListByTime(setting["WagerUrl"], setting["AgentAccount"], setting["APIkey"], startTime, endTime, page, guid);

                    var errorCode = dataWager.codeId;
                    if (errorCode != 0)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return false;
                    }

                    totalPage = dataWager.data.pageCount;
                    int totalNum = dataWager.data.rowCount;

                    if (errorCode == 0 && totalPage == 0)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return true;
                    }
                    // db wager count equals api bet count do nothing
                    if (totalNum == wagerCount)
                    {
                        msg = "MATCH";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return true;
                    }

                    // insert wager to db
                    foreach (var bet in dataWager.data.records)
                    {
                        try
                        {
                            _wagerDGService.InsertWagerDG(bet);
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.id} )";
                            _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                            return false;
                        }
                    }
                    page++;

                    // sleep 15 sec
                    Thread.Sleep(15000);
                }

                msg = "FINISH";
                _logger.LogInformation($"RedoWagersAsync {msg}\r\n{startTime} ~ {endTime}");
                return false;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError($"RedoWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }
    }
}
