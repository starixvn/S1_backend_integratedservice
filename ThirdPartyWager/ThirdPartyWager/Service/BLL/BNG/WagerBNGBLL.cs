using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL.BNG
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
            var recordList = await _apiBNGBLL.GetRecordListAsync(setting, startTime, endTime);

            if (recordList.Records != null)
            {
                foreach (var record in recordList.Records)
                {
                    try
                    {
                        await _wagerBNGService.InsertWager(record);
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(recordID: {record.recordID} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(record)}\r\n{ex.Message}");
                    }
                }
            }
        }
    }
}
