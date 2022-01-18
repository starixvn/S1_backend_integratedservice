using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoAllBetBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiAllBetBLL _apiAllBetBLL;
        private readonly WagerAllBetService _wagerAllBetService;
        private readonly ILogger<WagerRedoAllBetBLL> _logger;

        private readonly int _tpId = (int)Tp.Allbet;
        private readonly string _tpName = Tp.Allbet.ToString();

        public WagerRedoAllBetBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiAllBetBLL = provider.GetRequiredService<ApiAllBetBLL>();
            _wagerAllBetService = provider.GetRequiredService<WagerAllBetService>();
            _logger = provider.GetRequiredService<ILogger<WagerRedoAllBetBLL>>();
        }

        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix)
        {
            string msg = "FATAL_ERROR";
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", _tpName);
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                // count interval wagers
                var getWagerCount = _wagerAllBetService.GetWagerTotal(startUnix, endUnix);
                int wagerCount = getWagerCount.wagerCount;

                // pagination
                int pageIndex = 1;
                int pageRowCnt = 1;
                while (pageRowCnt >= 1)
                {
                    // bet 
                    var dataWager = await _apiAllBetBLL.GetBetListByTime(setting["ApiUrl"], setting["DesKey"], setting["MD5Key"], setting["PropertyId"], startTime, endTime, pageIndex, guid);
                    if (dataWager == null)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return false;
                    }

                    // error condition
                    var errorCode = dataWager.error_code;
                    if (errorCode != "OK")
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return false;
                    }

                    pageRowCnt = dataWager.page.datas.Count();
                    int totalNum = dataWager.page.count;
                    if (pageRowCnt == 0)
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
                    foreach (var bet in dataWager.page.datas)
                    {
                        try
                        {
                            var r = _wagerAllBetService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.betNum} )";
                            _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                            return false;
                        }
                    }
                    pageIndex++;
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
