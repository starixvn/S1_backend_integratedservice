using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerWonCasinoBLL
    {
        private readonly ILogger<WagerWonCasinoBLL> _logger;
        private readonly ApiWonCasinoBLL _apiWonCasinoBLL;
        private readonly WagerWonCasinoService _wagerWonCasinoService;
        public WagerWonCasinoBLL(IServiceProvider provider)
        {
            _logger = provider.GetService<ILogger<WagerWonCasinoBLL>>();
            _apiWonCasinoBLL = provider.GetRequiredService<ApiWonCasinoBLL>();
            _wagerWonCasinoService = provider.GetRequiredService<WagerWonCasinoService>();
        }

        public async Task GetWagerAsync(WonCasinoConfig setting, long startTime, long endTime)
        {
            string msg;

            foreach (var item in setting.CurrencyList)
            {
                var page = 1;
                var totalPage = 1;

                while (page <= totalPage)
                {
                    //page index從0開始所以-1
                    var wagerList = await _apiWonCasinoBLL.GetWagerAsync(item, setting.ApiUrl, startTime, endTime, page - 1);

                    if (wagerList.result == 0)
                    {
                        foreach (var wager in wagerList.array)
                        {
                            try
                            {
                                await _wagerWonCasinoService.InsertWager(wager);
                            }
                            catch (Exception ex)
                            {
                                msg = $"INSERT_WAGER(serialnumber: {wager.serialnumber}, id: {wager.id}, state: {wager.state} )";
                                _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(wager)}\r\n{ex.Message}");
                            }
                        }

                        //1個page取2000筆注單資料
                        totalPage = (wagerList.total / 2000) + 1;
                        page++;
                    }
                    else
                    {
                        break;
                    }
                }                   
            }
        }
    }
}
