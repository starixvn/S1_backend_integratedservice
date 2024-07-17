using Newtonsoft.Json;

using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerTFGBLL
    {
        private readonly ApiTFGBLL _apiTFGBLL;
        private readonly WagerTFGService _wagerTFGService;
        private readonly ILogger<ApiTFGBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        public WagerTFGBLL(IServiceProvider provider)
        {
            _apiTFGBLL = provider.GetRequiredService<ApiTFGBLL>();
            _wagerTFGService = provider.GetRequiredService<WagerTFGService>();
            _logger = provider.GetRequiredService<ILogger<ApiTFGBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        public async Task<bool> GetWagersAsync(string apiUrl, string privateToken, string startTime, string endTime)
        {
            string msg;
            try
            {
                var dataWager = await _apiTFGBLL.GetBetTransactionByTime(apiUrl, privateToken, startTime, endTime);
                if (dataWager == null)
                    return true;
                foreach (var wager in dataWager.results)
                {
                    try
                    {
                        var wagerTFG = new WagerTFGModel()
                        {
                            id = wager.id,
                            odds = wager.odds,
                            currency = wager.currency,
                            amount = wager.amount,
                            game_market_name = string.IsNullOrEmpty(wager.game_market_name) ? string.Empty : wager.game_market_name,
                            market_option = string.IsNullOrEmpty(wager.market_option) ? string.Empty : wager.market_option,
                            map_num = string.IsNullOrEmpty(wager.map_num) ? string.Empty : wager.map_num,
                            bet_type_name = string.IsNullOrEmpty(wager.bet_type_name) ? string.Empty : wager.bet_type_name,
                            event_id = wager.event_id,
                            event_datetime = wager.event_datetime == null ? DateTime.MinValue : wager.event_datetime?.DateTime,
                            date_created = wager.date_created == null ? DateTime.MinValue : wager.date_created?.DateTime,
                            settlement_datetime = wager.settlement_datetime == null ? DateTime.MinValue : wager.settlement_datetime?.DateTime,
                            modified_datetime = wager.modified_datetime == null ? DateTime.MinValue : wager.modified_datetime?.DateTime,
                            settlement_status = wager.settlement_status,
                            result_status = string.IsNullOrEmpty(wager.result_status) ? string.Empty : wager.result_status,
                            earnings = wager.earnings == null ? 0 : wager.earnings,
                            is_combo = wager.is_combo,
                            member_code = wager.member_code,
                            is_unsettled = wager.is_unsettled,
                            malay_odds = wager.malay_odds == null ? 0 : wager.malay_odds,
                            euro_odds = wager.euro_odds == null ? 0 : wager.euro_odds,
                            member_odds = wager.member_odds,
                            member_odds_style = wager.member_odds_style,
                            request_source = wager.request_source,
                            tickets = wager.is_combo ? JsonConvert.SerializeObject(wager.tickets) : "{}"
                        };

                        var result = _wagerTFGService.InsertWager(wagerTFG);

                        if (result.IsFaulted)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {wager.id} )";
                            _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(wager)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {wager.id} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(wager)}\r\n{ex.Message}");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<(bool Success, bool DoQuick)> UnsettleCheck(TFGConfig config)
        {
            try
            {
                string msg;
                var apiUrl = config.ApiUrl;
                var checkList = await _wagerTFGService.GetUnsettleCheckList();
                _logger.LogInformation($"TFG Unsettle Count:{checkList.Count()}");
                var doQuick = checkList.Count() == 1000;
                foreach (var chcek in checkList)
                {
                    _logger.LogInformation($"TFG id:{chcek.TransId}");
                    try
                    {
                        var bettrans = await _wagerTFGService.GetBettrans(chcek.TransId);

                        //當初config檔的人民幣跟三方取名不同，這邊做轉換
                        var currency = bettrans.currency == "RMB" ? "CNY" : bettrans.currency;
                        if (bettrans != null && config.CurrencyData.ContainsKey(currency))
                        {
                            var dataWager = await _apiTFGBLL.GetBetTransactionById(apiUrl, config.CurrencyData[currency].PrivateToken, chcek.TransId);
                            if (dataWager != null)
                            {
                                foreach (var wager in dataWager.results)
                                {
                                    try
                                    {
                                        var wagerTFG = new WagerTFGModel()
                                        {
                                            id = wager.id,
                                            odds = wager.odds,
                                            currency = wager.currency,
                                            amount = wager.amount,
                                            game_market_name = string.IsNullOrEmpty(wager.game_market_name) ? string.Empty : wager.game_market_name,
                                            market_option = string.IsNullOrEmpty(wager.market_option) ? string.Empty : wager.market_option,
                                            map_num = string.IsNullOrEmpty(wager.map_num) ? string.Empty : wager.map_num,
                                            bet_type_name = string.IsNullOrEmpty(wager.bet_type_name) ? string.Empty : wager.bet_type_name,
                                            event_id = wager.event_id,
                                            event_datetime = wager.event_datetime == null ? DateTime.MinValue : wager.event_datetime?.DateTime,
                                            date_created = wager.date_created == null ? DateTime.MinValue : wager.date_created?.DateTime,
                                            settlement_datetime = wager.settlement_datetime == null ? DateTime.MinValue : wager.settlement_datetime?.DateTime,
                                            modified_datetime = wager.modified_datetime == null ? DateTime.MinValue : wager.modified_datetime?.DateTime,
                                            settlement_status = wager.settlement_status,
                                            result_status = string.IsNullOrEmpty(wager.result_status) ? string.Empty : wager.result_status,
                                            earnings = wager.earnings == null ? 0 : wager.earnings,
                                            is_combo = wager.is_combo,
                                            member_code = wager.member_code,
                                            is_unsettled = wager.is_unsettled,
                                            malay_odds = wager.malay_odds == null ? 0 : wager.malay_odds,
                                            euro_odds = wager.euro_odds == null ? 0 : wager.euro_odds,
                                            member_odds = wager.member_odds,
                                            member_odds_style = wager.member_odds_style,
                                            request_source = wager.request_source,
                                            tickets = wager.is_combo ? JsonConvert.SerializeObject(wager.tickets) : "{}"
                                        };

                                        var result = _wagerTFGService.InsertWager(wagerTFG);

                                        if (result.IsFaulted)
                                        {
                                            msg = $"INSERT_WAGER(WAGERID: {wager.id} )";
                                            _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(wager)}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        msg = $"INSERT_WAGER(WAGERID: {wager.id} )";
                                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(wager)}\r\n{ex.Message}");
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogInformation($"TFG dataWager is null id:{chcek.TransId}");
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"TFG bettrans is null id:{chcek.TransId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"TFG Settle Check FATAL_ERROR.\r\n{ex.Message}");
                        return (false, true);
                    }
                }
                _logger.LogInformation($"TFG Settle Check Success.");
                return (true, doQuick);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TFG Settle Check FATAL_ERROR.\r\n{ex.Message}");
                return (false, true);
            }
        }
    }
}
