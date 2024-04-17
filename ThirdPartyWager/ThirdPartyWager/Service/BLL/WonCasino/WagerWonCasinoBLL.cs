using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerWonCasinoBLL
    {
        private readonly ILogger<WagerWonCasinoBLL> _logger;
        private readonly ApiWonCasinoBLL _apiWonCasinoBLL;
        private readonly WagerWonCasinoService _wagerWonCasinoService;
        private readonly TpUnsettleService _tpUnsettleService;
        public WagerWonCasinoBLL(IServiceProvider provider)
        {
            _logger = provider.GetService<ILogger<WagerWonCasinoBLL>>();
            _apiWonCasinoBLL = provider.GetRequiredService<ApiWonCasinoBLL>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _wagerWonCasinoService = provider.GetRequiredService<WagerWonCasinoService>();
        }

        public async Task<int> GetWagerAsync(WonCasinoConfig setting, long startTime, long endTime)
        {
            string msg;
            var successCount = 0;

            foreach (var item in setting.CurrencyList)
            {
                var page = 1;
                var totalPage = 1;

                while (page <= totalPage)
                {
                    //page index從0開始所以-1
                    var wagerList = await _apiWonCasinoBLL.GetWagerAsync(item, setting.ApiUrl, startTime, endTime, page - 1);

                    //结果信息(0成功，<0失败)
                    if (wagerList.result < 0)
                    {
                        //TP失敗 直接歸零回傳
                        msg = $"WonCasino_GetWagerAsync_THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(wagerList)}");
                        successCount = 0;
                        return successCount;
                    }
                    
                    if (wagerList.result == 0)
                    {
                        foreach (var wager in wagerList.array)
                        {
                            try
                            {
                                var result = await _wagerWonCasinoService.InsertWager(wager);
                                if (result >= 1)
                                {
                                    _ = _tpUnsettleService.InsertTpUnsettle(Tp.WonCasino, GameType.WonCasino_LiveCasino, wager.id.ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                msg = $"INSERT_WAGER(serialnumber: {wager.serialnumber}, id: {wager.id}, state: {wager.state} )";
                                _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(wager)}\r\n{ex.Message}");
                                //INSERT失敗到直接歸零回傳
                                successCount = 0;
                                return successCount;
                            }
                        }
                        
                        //1個page取2000筆注單資料
                        totalPage = (wagerList.total / 2000) + 1;
                        page++;
                        //TP 限制一秒以上
                        //3秒後在抓下一個currency
                        Thread.Sleep(3000);
                    }
                    else
                    {
                        break;
                    }
                }     
                //有執行成功就加
                successCount++;
            }
            return successCount;
        }
    }
}
