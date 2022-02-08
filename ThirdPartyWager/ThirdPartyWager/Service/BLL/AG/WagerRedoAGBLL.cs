using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoAGBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiAGBLL _apiAGBLL;
        private readonly WagerAGService _wagerAGService;
        private readonly ILogger<WagerAGBLL> _logger;

        private readonly int _tpId = (int)Tp.AG;

        public WagerRedoAGBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiAGBLL = provider.GetRequiredService<ApiAGBLL>();
            _wagerAGService = provider.GetRequiredService<WagerAGService>();
            _logger = provider.GetRequiredService<ILogger<WagerAGBLL>>();
        }

        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix)
        {
            string msg = "FATAL_ERROR";
            string guid = Guid.NewGuid().ToString();

            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                // count interval wagers
                var getWagerCount = _wagerAGService.GetWagerTotal(startUnix, endUnix);
                int wagerCount = getWagerCount.wagerCount;

                var getWagerSummary = await _apiAGBLL.GetBetSummaryByTime(setting["WagerUrl"], setting["HashKey"], setting["data_cagent"], startTime, endTime, guid);
                var arrSummary = getWagerSummary.result;

                // error condition
                var errorCode = arrSummary.info;
                if (errorCode != 0)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    return false;
                }

                if (errorCode == 0 && arrSummary.row == null)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    return true;
                }

                // db wager count equals api bet count do nothing
                var totalNum = 0;
                foreach (var data in arrSummary.row)
                {
                    totalNum += data.wagersTotal;
                }
                if (totalNum == wagerCount)
                {
                    msg = "MATCH";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    return true;
                }

                // parse 10 minute
                var st = Convert.ToDateTime(startTime);
                var ed = Convert.ToDateTime(startTime).AddMinutes(10).AddMilliseconds(-1);
                var bg = "";
                var fn = "";
                for (int i = 1; i <= 6; i++)
                {
                    int page = 1;
                    int totalPage = 1;
                    if (i > 1)
                    {
                        st = st.AddMinutes(10);
                        ed = ed.AddMinutes(10);
                    }
                    bg = st.ToString("yyyy-MM-dd HH:mm:ss");
                    fn = ed.ToString("yyyy-MM-dd HH:mm:ss");
                    while (page <= totalPage)
                    {
                        var dataWager = await _apiAGBLL.GetBetListByTime(setting["WagerUrl"], setting["HashKey"], setting["data_cagent"], bg, fn, page, guid);
                        var arrWager = dataWager.result;

                        // error condition
                        errorCode = arrWager.info;
                        if (errorCode != 0)
                        {
                            msg = "THIRD_PARTY_ERROR";
                            _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                            return false;
                        }

                        // pagination
                        var pageInfo = arrWager.addition;
                        var totalNo = pageInfo.total;
                        totalPage = pageInfo.totalpage;
                        if (totalNo == 0)
                        {
                            msg = "NO_DATA";
                            _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                            continue;
                        }

                        foreach (var bet in arrWager.row)
                        {
                            try
                            {
                                _wagerAGService.InsertWager(bet);
                            }
                            catch (Exception ex)
                            {
                                msg = $"INSERT_WAGER(WAGERID: {bet.billNo} )";
                                _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                                return false;
                            }
                        }
                        page++;
                    }
                }

                msg = "FINISH_REDO";
                _logger.LogInformation($"RedoWagersAsync {msg}\r\n{startTime} ~ {endTime}");
                return false;

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
