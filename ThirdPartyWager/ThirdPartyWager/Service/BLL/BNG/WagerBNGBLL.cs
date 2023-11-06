using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerBNGBLL
    {
        private readonly ILogger<WagerBNGBLL> _logger;
        private readonly ApiBNGBLL _apiBNGBLL;
        private readonly WagerBNGService _wagerBNGService;
        public WagerBNGBLL(IServiceProvider provider)
        {
            _logger = provider.GetService<ILogger<WagerBNGBLL>>();
            _apiBNGBLL = provider.GetRequiredService<ApiBNGBLL>();
            _wagerBNGService = provider.GetRequiredService<WagerBNGService>();
        }

        public async Task GetWagerAsync(Dictionary<string, string> setting, string startTime, string endTime)
        {
            string msg;
            var wagerList = await _apiBNGBLL.GetRecordListAsync(setting, startTime, endTime);

            if (wagerList.items != null)
            {
                foreach (var item in wagerList.items)
                {
                    try
                    {
                        await _wagerBNGService.InsertWager(item);
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(round_id: {item.round_id}, transaction_id: {item.transaction_id} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(item)}\r\n{ex.Message}");
                    }
                }
            }
        }
    }
}
