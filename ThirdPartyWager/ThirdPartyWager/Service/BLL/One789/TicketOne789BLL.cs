using Newtonsoft.Json;

using Serilog.Context;

using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service
{
    public class TicketOne789BLL
    {
        private readonly ApiOne789BLL _apiOne789BLL;
        private readonly WagerOne789Service _wagerOne789Service;
        private readonly ILogger<TicketOne789BLL> _logger;

        public TicketOne789BLL(IServiceProvider provider)
        {
            _apiOne789BLL = provider.GetRequiredService<ApiOne789BLL>();
            _wagerOne789Service = provider.GetRequiredService<WagerOne789Service>();
            _logger = provider.GetRequiredService<ILogger<TicketOne789BLL>>();
        }

        // TpRefundWorker
        public IEnumerable<BettransDataOne789> GetPendingList(int gameType)
        {
            return _wagerOne789Service.GetPendingList(gameType);
        }

        // TpRefundWorker
        public async Task<bool> CheckTicketStatusAsync(dynamic setting, dynamic pending, int gameType)
        {
            LogContext.PushProperty("SystemJob", $"Auto-Dispatch");
            var func = "CheckTicketStatusAsync";
            var msg = "";

            // check bettrans status
            BettransDataOne789 bettrans = _wagerOne789Service.GetBettransData(gameType, pending.Tx);
            if (bettrans.TicketStatus == (int)TicketStatus.Refund)
            {
                _logger.LogWarning($"Refunded : {pending.Tx}");
                return true;
            }

            // check one789 status
            var param = new List<string> { pending.Tx };

            var data = await _apiOne789BLL.CheckTicketStatus(setting, param);
            TicketDataOne789 ticketData = JsonConvert.DeserializeObject<TicketDataOne789>(data);

            if (ticketData.ErrorCode != null)
            {
                msg = $"{func} THIRD_PARTY_ERROR";
                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(ticketData)}");
                return false;
            }

            // data not exists 
            if (ticketData.Result.Count == 0)
            {
                // call sw api
                try
                {
                    var response = _apiOne789BLL.callSeamlessApi("Refund", pending).Result;
                    if (response == null)
                    {
                        msg = $"{func} SEAMLESS_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(pending)}\r\nresponse={JsonConvert.SerializeObject(response)}");
                        return false;
                    }

                    SeamlessOne789Response result = JsonConvert.DeserializeObject<SeamlessOne789Response>(response);
                    if (result == null || result.ErrorCode != null)
                    {
                        msg = $"{func} SEAMLESS_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(pending)}\r\nresult={JsonConvert.SerializeObject(result)}");
                        return false;
                    }
                    else if (result.Tx == pending.Source)
                    {
                        _logger.LogInformation($"System Refund : {pending.Tx}");
                    }
                    else
                    {
                        msg = $"{func} UNKNOW_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(pending)}\r\nresult={JsonConvert.SerializeObject(result)}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
            else
            {
                _logger.LogError($"Refund Not Match: {pending.Tx} {JsonConvert.SerializeObject(ticketData)}");
                return false;
            }

            return true;
        }
    }
}
