using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerDragonGamingBLL
    {

        private readonly ApiDragonGamingBLL _apiDragonGamingBLL;
        private readonly WagerDragonGamingService _wagerDragonGamingService;
        private readonly ILogger<WagerDragonGamingBLL> _logger;

        public WagerDragonGamingBLL(IServiceProvider provider)
        {
            _apiDragonGamingBLL = provider.GetRequiredService<ApiDragonGamingBLL>();
            _wagerDragonGamingService = provider.GetRequiredService<WagerDragonGamingService>();
            _logger = provider.GetService<ILogger<WagerDragonGamingBLL>>();
        }

        public async Task<bool> GetWagerAsync(DragonGamingConfig setting, string startTime, string endTime)
        {
            string msg;

            var result = await _apiDragonGamingBLL.GetWagerAsync(setting.ApiUrl, setting.ApiKey, startTime, endTime);

            if (result.HasError)
            {
                msg = $"DragonGaming_GetWagerAsync_THIRD_PARTY_ERROR";
                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(result)}");
                return false;
            }

            if (result.result.game_history.data.Count == 0)
            {
                msg = $"DragonGaming_GetWagerAsync_THIRD_PARTY_NO_DATA";
                _logger.LogWarning($"{msg}\r\n{JsonConvert.SerializeObject(result)}");
                return true;
            }

            var data = ConvertResponseData(result.result.game_history.data);

            foreach (var wager in data)
            {
                try
                {
                    var insertResult = await _wagerDragonGamingService.InsertWager(new WagerDragonGamingModel(wager));
                }
                catch (Exception ex)
                {
                    return LogAndReturnFailure(wager, ex);
                }
            }

            _logger.LogInformation($"DragonGaming_GetWagerAsync_SUCCESS, COUNT={data.Count()}");

            return true;
        }

        private IEnumerable<GameHistoryData> ConvertResponseData(List<List<object>> data)
        {
            List<GameHistoryData> convertDataList = new List<GameHistoryData>();

            foreach (var item in data)
            {
                var gameHistoryItem = new GameHistoryData
                {
                    PlayerID = item[0]?.ToString(),
                    GameID = item[1]?.ToString(),
                    GameName = item[2]?.ToString(),
                    GameCategory = item[3]?.ToString(),
                    Platform = item[4]?.ToString(),
                    RoundID = item[5]?.ToString(),
                    BetAmount = Convert.ToDecimal(item[6]),
                    WinAmount = Convert.ToDecimal(item[7]),
                    AmountType = item[8]?.ToString(),
                    PlayType = item[9]?.ToString(),
                    DateTime = item[10]?.ToString(),
                    BetStatus = item[11]?.ToString()
                };

                convertDataList.Add(gameHistoryItem);
            }

            return convertDataList;
        }

        private bool LogAndReturnFailure(GameHistoryData data, Exception ex = null)
        {
            string msg = $"INSERT_WAGER(playerId: {data.PlayerID}, roundid: {data.RoundID}, gameid: {data.GameID} )";
            if (ex == null)
            {
                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(data)}\r\nresult = -1");
            }
            else
            {
                _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(data)}\r\n{ex.Message}");
            }
            return false;
        }
    }
}
