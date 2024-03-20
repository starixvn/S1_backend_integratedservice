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
                var wagerList = await _apiWonCasinoBLL.GetWagerAsync(item, setting.ApiUrl, startTime, endTime);

                if (wagerList.result == 0)
                {
                    foreach(var wager in wagerList.array)
                    {
                        try
                        {
                            await _wagerWonCasinoService.InsertWager(wager);
                        }
                        catch(Exception ex)
                        {
                            msg = $"INSERT_WAGER(serialnumber: {wager.serialnumber}, id: {wager.id}, state: {wager.state} )";
                            _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(wager)}\r\n{ex.Message}");
                        }
                    }
                }
            }

        }
    }
}
