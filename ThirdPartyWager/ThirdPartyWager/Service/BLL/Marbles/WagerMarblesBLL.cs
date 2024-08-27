using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerMarblesBLL
    {
        private readonly ILogger<WagerMarblesBLL> _logger;
        private readonly ApiMarblesBLL _apiMarbleBLL;
        private readonly WagerMarblesService _wagerMarblesService;
        private readonly TpUnsettleService _tpUnsettleService;
        public WagerMarblesBLL(IServiceProvider provider)
        {
            _logger = provider.GetService<ILogger<WagerMarblesBLL>>();
            _apiMarbleBLL = provider.GetRequiredService<ApiMarblesBLL>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _wagerMarblesService = provider.GetRequiredService<WagerMarblesService>();
        }

        public async Task<int> GetWagerAsync(MarblesConfig setting, long startTime, long endTime)
        {
            var msg = "FATAL_ERROR";
            var page = 1;
            var totalPage = 1;
            var successCount = 0;

            while (page <= totalPage)
            {
                var wagerList = await _apiMarbleBLL.GetMarblesWagerAsync(1, setting, startTime, endTime, page);
                //结果信息(0成功，不是0都失败)
                if (wagerList.code != "0")
                {
                    //TP失敗 直接-1回傳
                    msg = $"Marbles_GetWagerAsync_THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(wagerList)}");
                    successCount = -1;
                    return successCount;
                }
    
                if (wagerList.code == "0")
                {
                    foreach (var wager in wagerList.data.bets)
                    {
                        try
                        {
                            var result = await _wagerMarblesService.InsertWager(wager);
                
                            //根據Marbles 結尾有-1的才是結算單 結算單再丟TpUnSettle
                            if (wager.transNo.EndsWith("-1"))
                            {
                                _ = _tpUnsettleService.InsertTpUnsettle(Tp.Marbles, GameType.Marbles_Lottery, wager.gameNo);
                            }
                        }
                        catch (Exception ex)
                        {
                            return LogAndReturnFailure(wager.gameNo, wager.transNo, wager, ex);
                        }
                    }
        
                    //1個page取5000筆注單資料
                    totalPage = (wagerList.data.total / 5000) + 1;
                    page++;
                    //有執行成功就加
                    successCount++;
                }
                else
                {
                    break;
                }
            }
            return successCount;
        }
        
        private int LogAndReturnFailure(string gameNo, string trasnNo, object wager, Exception ex = null)
        {
            string msg = $"INSERT_WAGER(gameNo: {gameNo}, trasnNo: {trasnNo}, )";
            if (ex == null)
            {
                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(wager)}\r\nresult = -1");
            }
            else
            {
                _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(wager)}\r\n{ex.Message}");
            }
            return -1;
        }
    }
}