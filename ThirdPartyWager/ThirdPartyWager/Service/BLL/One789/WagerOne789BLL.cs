using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerOne789BLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiOne789BLL _apiOne789BLL;
        private readonly WagerOne789Service _wagerOne789Service;
        private readonly ILogger<WagerOne789BLL> _logger;
        private readonly One789Config _siteConfig;
        private readonly TpUnsettleService _tpUnsettleService;
        public WagerOne789BLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiOne789BLL = provider.GetRequiredService<ApiOne789BLL>();
            _wagerOne789Service = provider.GetRequiredService<WagerOne789Service>();
            _logger = provider.GetRequiredService<ILogger<WagerOne789BLL>>();
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<One789Config>(Tp.One789.ToString());
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime, int gameType)
        {
            string msg;
            try
            {
                // get api info
                var setting = _siteConfig;

                var page = 1;
                var totalPage = 1;
                var perPage = 500;
                while (page <= totalPage)
                {
                    // get wager
                    var dataWager = await _apiOne789BLL.GetBetListByTime(setting, startTime, endTime, page, perPage, gameType);

                    if (dataWager.Count > 0 && dataWager[0]["ErrorCode"] != null)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return false;
                    }
                    if (dataWager.Count == 0)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return true;
                    }

                    totalPage = (dataWager.Count == perPage) ? totalPage + 1 : totalPage;
                    foreach (var bet in dataWager)
                    {
                        try
                        {
                            var record = JsonConvert.DeserializeObject<BetOne789>(JsonConvert.SerializeObject(bet));
                            if (gameType == (int)GameType.One789_LiveCasino)
                            {
                                record.TransId = $"{record.AreaNumber}_{record.GameNumber}_{record.GameRound}_{record.Username}";
                                record.GameResult = (record.Status == 0) ? -1 : record.GameResult;
                            }
                            else
                            {
                                record.Numbers = JsonConvert.SerializeObject(record.Numbers);
                                record.TransId = $"{record.Term}_{record.GameType}_{record.Username}";
                            }
                            var result = _wagerOne789Service.InsertWagerOne789(record, gameType);

                            if (result)
                            {
                                _tpUnsettleService.InsertTpUnsettle(Tp.One789, (GameType)gameType, record.TransId);
                            }
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER";
                            _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                            return false;
                        }
                    }
                    page++;
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
    }
}
