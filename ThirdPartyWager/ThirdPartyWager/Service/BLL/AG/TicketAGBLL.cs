using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class TicketAGBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiAGBLL _apiAGBLL;
        private readonly WagerAGService _wagerAGService;
        private readonly ILogger<TicketAGBLL> _logger;

        private readonly int _tpId = (int)Tp.AG;
        private readonly string _tpName = Tp.AG.ToString();

        public TicketAGBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiAGBLL = provider.GetRequiredService<ApiAGBLL>();
            _wagerAGService = provider.GetRequiredService<WagerAGService>();
            _logger = provider.GetRequiredService<ILogger<TicketAGBLL>>();
        }

        // TpRefundWorker
        public IEnumerable<BettransDataAG> GetPendingList(int gameType)
        {
            return _wagerAGService.GetPendingList();
        }

        // TpRefundWorker
        public async Task<bool> CheckTicketStatusAsync(dynamic setting, dynamic pending)
        {
            LogContext.PushProperty("SystemJob", $"Auto-Dispatch");
            LogContext.PushProperty("Tp", _tpName);
            string func = "CheckTicketStatusAsync";
            var msg = "";
            bool task = true;

            var data = await _apiAGBLL.CheckTicketStatus(setting["CommonUrl"], pending.transactionID);

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
