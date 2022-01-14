using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        private readonly int _tpId = (int)Tp.One789;
        private readonly string _tpName = Tp.One789.ToString();

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
        public async Task<bool> CheckTicketStatusAsync(dynamic setting, dynamic pending)
        {
            LogContext.PushProperty("Tp", _tpName);
            string func = "CheckTicketStatusAsync";
            _logger.LogError($"Data Need Refund: {JsonConvert.SerializeObject(pending)}");
            return true;
        }
    }
}
