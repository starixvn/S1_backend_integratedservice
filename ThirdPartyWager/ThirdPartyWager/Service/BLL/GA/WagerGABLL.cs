using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerGABLL
    {
        private readonly ILogger<WagerGABLL> _logger;
        private readonly ApiGABLL _apiGABLL;
        private readonly WagerGAService _wagerGAService;
        public WagerGABLL(IServiceProvider provider)
        {
            _logger = provider.GetService<ILogger<WagerGABLL>>();
            _apiGABLL = provider.GetRequiredService<ApiGABLL>();
            _wagerGAService = provider.GetRequiredService<WagerGAService>();
        }

        public async Task GetWagerAsync(GAConfig setting, string startTime, string endTime)
        {
            string msg;
            var page = 1;
            var totalPage = 1;

            while (page <= totalPage)
            {
                var wagerList = await _apiGABLL.GetRecordListAsync(setting, startTime, endTime, page);

                if (wagerList?.Any() ?? false)
                {
                    //GA一次最多只能撈32筆
                    if (wagerList.Count == 32)
                        totalPage++;

                    foreach (var item in wagerList)
                    {
                        try
                        {
                            if (item.settled_date_time != null)
                            {
                                await _wagerGAService.InsertWager(item);
                            }
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(sid: {item.sid}, status: {item.status} )";
                            _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(item)}\r\n{ex.Message}");
                        }
                    }
                }

                page++;
            }

        }
    }
}
