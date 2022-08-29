using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerAllBetBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiAllBetBLL _apiAllBetBLL;
        private readonly WagerAllBetService _wagerAllBetService;
        private readonly ILogger<WagerAllBetBLL> _logger;
        private IEnumerable<TpCurrency> _currencyList;
        private readonly int _tpId = (int)Tp.Allbet;
        private readonly IWebHostEnvironment _env;

        public WagerAllBetBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiAllBetBLL = provider.GetRequiredService<ApiAllBetBLL>();
            _wagerAllBetService = provider.GetRequiredService<WagerAllBetService>();
            _logger = provider.GetRequiredService<ILogger<WagerAllBetBLL>>();
            _currencyList = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList(_tpId);
            _env = provider.GetRequiredService<IWebHostEnvironment>();
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime)
        {
            var msg = "FATAL_ERROR";
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);
                if (_env.IsDevelopment())
                {
                    _currencyList = _currencyList.Take(1);
                }
                foreach (var currency in _currencyList)
                {
                    try
                    {
                        Thread.Sleep(15000);
                        // pagination
                        var pageIndex = 1;
                        var pageRowCnt = 1;

                        var agent = setting[$"{currency.TpCurrencyId}Agent"];
                        while (pageRowCnt >= 1)
                        {
                            // bet                     
                            var dataWager = await _apiAllBetBLL.GetBetListByTime(setting, agent, startTime, endTime, pageIndex);
                            if (dataWager == null)
                            {
                                msg = "THIRD_PARTY_ERROR";
                                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                                return false;
                            }

                            // error condition
                            if (dataWager.resultCode != "OK")
                            {
                                msg = "THIRD_PARTY_ERROR";
                                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                                return false;
                            }

                            pageRowCnt = dataWager.data.list.Count();
                            if (pageRowCnt == 0)
                            {
                                msg = "NO_DATA";
                                _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                                break;
                            }

                            // insert wager to db
                            foreach (var bet in dataWager.data.list.OrderBy(item => item.betTime))
                            {
                                try
                                {
                                    _wagerAllBetService.InsertWager(bet);
                                }
                                catch (Exception ex)
                                {
                                    msg = $"INSERT_WAGER(WAGERID: {bet.betNum} )";
                                    _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                                    return false;
                                }
                            }
                            pageIndex++;
                        }

                        
                        // modified bet
                        var modifyWager = await _apiAllBetBLL.GetModifiedBetListByTime(setting, agent, startTime, endTime);
                        if (modifyWager.resultCode != "OK")
                        {
                            msg = "THIRD_PARTY_ERROR";
                            _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(modifyWager)}");
                            return false;
                        }

                        var pageRowCntOfModify = modifyWager.data.total;
                        if (pageRowCntOfModify == 0)
                        {
                            msg = "NO_DATA";
                            _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(modifyWager)}");
                            break;
                        }

                        // insert wager to db
                        foreach (var bet in modifyWager.data.list)
                        {
                            try
                            {
                                _wagerAllBetService.InsertWager(bet);
                            }
                            catch (Exception ex)
                            {
                                msg = $"INSERT_WAGER(WAGERID: {bet.betNum} )";
                                _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{currency.TpCurrencyId}Agent GetWagersAsync error. \r\n{ex.Message}");
                    }
                }
                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{startTime} ~ {endTime}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }

        public async Task<(bool Success, bool DoQuick)> UnsettleCheck()
        {
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(_tpId);

                var checkList = await _wagerAllBetService.GetUnsettleCheckList();
                var doQuick = checkList.Count() == 200;
                foreach (var chcek in checkList)
                {
                    try
                    {
                        var result = await _apiAllBetBLL.GetBetByBetNum(setting, chcek.betNum);
                        if (result.resultCode == "OK")
                        {
                            _wagerAllBetService.InsertWager(result.data);
                        }

                    }
                    catch (Exception ex)
                    {
                        var msg = $"INSERT WAGER Error betNum:{chcek.betNum}.";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(chcek)}\r\n{ex.Message}");
                    }
                }

                _logger.LogInformation($"Allbet Settle Check Success.");
                return (true, doQuick);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Allbet Settle Check FATAL_ERROR.\r\n{ex.Message}");
                return (false, true);
            }
        }
    }
}
