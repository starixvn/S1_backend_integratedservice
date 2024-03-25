using Newtonsoft.Json;

using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerKiss918BLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiKiss918BLL _apiKiss918BLL;
        private readonly WagerKiss918Service _wagerKiss918Service;
        private readonly ILogger<WagerKiss918BLL> _logger;

        public WagerKiss918BLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiKiss918BLL = provider.GetRequiredService<ApiKiss918BLL>();
            _wagerKiss918Service = provider.GetRequiredService<WagerKiss918Service>();
            _logger = provider.GetRequiredService<ILogger<WagerKiss918BLL>>();
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime)
        {
            var msg = "FATAL_ERROR";
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                //convert timepoint
                var timepoint = await _apiKiss918BLL.GetTimepoint(setting["ApiToken"], setting["ApiUrl"], startTime);

                if (string.IsNullOrWhiteSpace(timepoint))
                {
                    msg = "GET_TIMEPOINT_ERROR";
                    _logger.LogError($"{msg}\r\n{timepoint}");
                    return false;
                }

                // get history bets
                var (hasData, dataDetails) = await _apiKiss918BLL.GetPlaySession(setting["AccessToken"], setting["ApiUrl"], timepoint);

                if (!hasData)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataDetails)}");
                    return false;
                }

                // get fail transactions history
                var (hasFailData, failDataDetails) = await _apiKiss918BLL.GetFailTransaction(setting["AccessToken"], setting["ApiUrl"], timepoint);

                if (hasData && dataDetails.Count() == 0)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataDetails)}");
                    return true;
                }

                try
                {
                    _wagerKiss918Service.InsertWager(dataDetails);
                    if (hasFailData && dataDetails.Count() > 0)
                        _wagerKiss918Service.InsertWager(failDataDetails);
                }
                catch (Exception ex)
                {
                    msg = $"INSERT_WAGER";
                    _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(dataDetails)}\r\n{ex.Message}");
                    return false;
                }
                //}
                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{startTime} ~ {dataDetails.MaxBy(x => x.created).created}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }

        public async Task<bool> GetWagersRedoAsync(int tpId, string startTime)
        {
            var msg = "REDO_FATAL_ERROR";
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                //convert timepoint
                var timepoint = await _apiKiss918BLL.GetTimepoint(setting["ApiToken"], setting["ApiUrl"], startTime);

                if (string.IsNullOrWhiteSpace(timepoint))
                {
                    msg = "REDO_GET_TIMEPOINT_ERROR";
                    _logger.LogError($"{msg}\r\n{timepoint}");
                    return false;
                }

                // get history bets
                var (hasData, dataDetails) = await _apiKiss918BLL.GetPlaySession(setting["AccessToken"], setting["ApiUrl"], timepoint);

                if (!hasData)
                {
                    msg = "REDO_THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataDetails)}");
                    return false;
                }

                if (hasData && dataDetails.Count() == 0)
                {
                    msg = "REDO_NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataDetails)}");
                    return true;
                }

                var (hasFailData, failDataDetails) = await _apiKiss918BLL.GetFailTransaction(setting["AccessToken"], setting["ApiUrl"], timepoint);

                try
                {
                    var lostData = new List<WagerKiss918Model>();
                    foreach (var data in dataDetails.Union(failDataDetails))
                    {
                        if ((await _wagerKiss918Service.GetWager(data.referenceID)).Any())
                            continue;
                        else
                            lostData.Add(data);
                    }
                    if (lostData.Count() == 0)
                    {
                        msg = "REDO_NO_LOST_DATA";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(lostData)}");
                    }
                    else
                    {
                        msg = "REDO_CHECK_LOST_DATA";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(lostData.Select(x => x.referenceID))}");
                        _wagerKiss918Service.InsertWager(lostData);
                    }
                }
                catch (Exception ex)
                {
                    msg = $"REDO_INSERT_WAGER";
                    _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(dataDetails)}\r\n{ex.Message}");
                    return false;
                }
                msg = "REDO_FINISH";
                _logger.LogInformation($"GetWagersRedoAsync {msg}\r\n{startTime} ~ {dataDetails.MaxBy(x => x.created).created}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "REDO_FATAL_ERROR";
                _logger.LogError(ex, $"GetWagersRedoAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }

    }

}
