using Newtonsoft.Json;

using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerSGBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiSGBLL _apiSGBLL;
        private readonly WagerSGService _wagerSGService;
        private readonly ILogger<WagerSGBLL> _logger;
        private readonly TpUnsettleService _tpUnsettleService;

        public WagerSGBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiSGBLL = provider.GetRequiredService<ApiSGBLL>();
            _wagerSGService = provider.GetRequiredService<WagerSGService>();
            _logger = provider.GetRequiredService<ILogger<WagerSGBLL>>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime)
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                // pagination
                var page = 1;
                var totalPage = 1;
                while (page <= totalPage)
                {
                    // get wager
                    var dataWager = await _apiSGBLL.GetBetListByTime(setting["ApiUrl"], setting["merchantCode"], startTime, endTime, page, guid);

                    // error condition
                    var errorCode = dataWager.code;

                    if (errorCode != 0)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return false;
                    }

                    var totalNum = dataWager.resultCount;
                    totalPage = dataWager.pageCount;
                    if (totalNum == 0)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return true;
                    }

                    // insert wager to db
                    foreach (var bet in dataWager.list)
                    {
                        try
                        {
                            var result = 0;
                            if (!string.IsNullOrEmpty(bet.categoryId) && bet.categoryId.Equals("BN", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            var gameType = GetGameType(bet.gameCode);
                            result = gameType is GameType.SG_RNG or GameType.SG_Table ? _wagerSGService.InsertWagerRng(bet) : _wagerSGService.InsertWager(bet);
                            
                            if (result > 0)
                            {
                                _tpUnsettleService.InsertTpUnsettle(Tp.SG, gameType, bet.referenceId);
                            }
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.ticketId} )";
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

        private GameType GetGameType(string gameCode)
        {
            if (gameCode.StartsWith("F-", StringComparison.OrdinalIgnoreCase))
            {
                return GameType.SG_Fish;
            }

            if (gameCode.StartsWith("C-", StringComparison.OrdinalIgnoreCase))
            {
                return GameType.SG_Table;
            }

            return GameType.SG_Fish;
        }
    }
}
