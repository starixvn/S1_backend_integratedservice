using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class TicketAGBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiAGBLL _apiAGBLL;
        private readonly WagerAGService _wagerAGService;
        private readonly ILogger<TicketAGBLL> _logger;
        private readonly Dictionary<int, AGConfig> _siteConfigDic;

        public TicketAGBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiAGBLL = provider.GetRequiredService<ApiAGBLL>();
            _wagerAGService = provider.GetRequiredService<WagerAGService>();
            _logger = provider.GetRequiredService<ILogger<TicketAGBLL>>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<AGConfig>(Tp.AG.ToString());
        }

        // TpRefundWorker
        public IEnumerable<BettransDataAG> GetPendingList(int gameType)
        {
            return _wagerAGService.GetPendingList();
        }

        // TpRefundWorker
        public async Task<bool> CheckTicketStatusAsync(dynamic pending, int gameType)
        {
            LogContext.PushProperty("SystemJob", $"Auto-Dispatch");
            // 取Site=0 共用網址
            var config = _siteConfigDic[0];
            var func = "CheckTicketStatusAsync";
            var task = true;

            var data = await _apiAGBLL.CheckTicketStatus(config.CommonUrl, pending.transactionID);

            string msg;
            if (data == null)
            {
                msg = $"{func} NO_DATA";
                _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(data)}");
                return task;
            }

            if (data.result.row[0].status == "3")
            {
                // call sw api
                try
                {
                    var response = _apiAGBLL.callSeamlessApi("Refund", pending).Result;
                    var result = JsonConvert.DeserializeObject<SeamlessAgResponse>(response);
                    if (result.TransferResponse.ResponseCode != "OK")
                    {
                        task = false;
                        msg = $"{func} SEAMLESS_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(result)}");
                    }
                    else
                    {
                        _logger.LogInformation($"System Refund : {pending.transactionID}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
            else
            {
                task = false;
                _logger.LogError($"Refund Not Match: {pending.transactionID} {JsonConvert.SerializeObject(data)}");
            }
            return task;
        }
    }
}
