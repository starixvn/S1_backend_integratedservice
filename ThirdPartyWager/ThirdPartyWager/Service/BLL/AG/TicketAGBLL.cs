using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
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

        public async Task<bool> ExecRefundWorker()
        {
            return await CheckTicketAsync();

        }

        public async Task<dynamic> CheckTicketAsync()
        {
            string func = "GetRefundAsync";
            string msg = "FATAL_ERROR";
            bool quick = false;
            LogContext.PushProperty("Tp", _tpName);
            try
            {
                var pendingList = _wagerAGService.GetPendingList();

                if (pendingList == null)
                {
                    return quick;
                }

                var setting = _thirdPartyBLL.GetThirdPartySetting(_tpId);
                setting["CommonUrl"] = "https://swapi.agingames.com";

                foreach (var pending in pendingList)
                {
                    var data = await _apiAGBLL.CheckTicketStatus(setting["CommonUrl"], pending.transactionID);

                    if (data == null)
                    {
                        msg = $"{func} NO_DATA";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(data)}");
                        return quick;
                    }

                    if (data.result.row[0].status == "3")
                    {
                        // call sw api
                        var response = _apiAGBLL.callSeamlessApi("Refund", pending).Result;
                        var result = JsonConvert.DeserializeObject<SeamlessAgResponse>(response);
                        if (result.TransferResponse.ResponseCode != "OK")
                        {
                            msg = $"{func} SEAMLESS_ERROR";
                            _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(result)}");
                        }
                        else
                        {
                            _logger.LogInformation($"System Refund : {pending.transactionID}");
                        }
                    }
                    else
                    {
                        _logger.LogError($"Refund Not Match: {pending.transactionID}\r\n{JsonConvert.SerializeObject(data)}");
                    }
                }

                msg = $"{func} FINISH";
                _logger.LogInformation(msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                quick = true;
            }
            return quick;
        }
    }
}
