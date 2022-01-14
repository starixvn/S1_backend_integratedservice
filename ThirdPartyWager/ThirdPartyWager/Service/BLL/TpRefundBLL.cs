using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class TpRefundBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<TpRefundBLL> _logger;
        private readonly IServiceProvider _provider;

        public TpRefundBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _logger = provider.GetRequiredService<ILogger<TpRefundBLL>>();
            _provider = provider;
        }

        public async Task<bool> ExecRefundWorker(int gameType)
        {
            dynamic _bll = "";
            int tpId = 0;
            try
            {
                // get bll
                switch (gameType)
                {
                    case (int)GameType.AG_LiveCasino:
                        tpId = (int)Tp.AG;
                        _bll = new TicketAGBLL(_provider);
                        break;
                    case (int)GameType.One789_Lottery:
                    case (int)GameType.One789_Lottery_789:
                    case (int)GameType.One789_LiveCasino:
                        tpId = (int)Tp.One789;
                        _bll = new TicketOne789BLL(_provider);
                        break;
                    default:
                        _logger.LogError($"Refund Not Support: {tpId}");
                        break;
                }

                if (tpId == 0)
                {
                    return true;
                }

                // get list
                var pendingList = _bll.GetPendingList(gameType);
                if (pendingList == null)
                {
                    return false;
                }

                // get setting
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                // process
                var result = true;
                foreach (var pending in pendingList)
                {
                    var check = _bll.CheckTicketStatusAsync(setting, pending).Result;
                    if (!check)
                    {
                        result = false;
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }
    }
}
