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

        public bool ExecRefundWorker(int gameType)
        {
            dynamic _bll = "";
            var tpId = 0;
            var result = true;
            try
            {
                // get bll
                switch (gameType)
                {
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
                var siteList = _thirdPartyBLL.GetTpListByTpId(tpId);
                // check tp status and only get first site to run
                if (!siteList.Any(x => x.Status == 1))
                {
                    return false;
                }

                var pendingList = _bll.GetPendingList(gameType);
                if (pendingList == null || pendingList.Count == 0)
                {
                    return false;
                }

                // process
                foreach (var pending in pendingList)
                {
                    var check = _bll.CheckTicketStatusAsync(pending, gameType).Result;
                    if (!check)
                    {
                        result = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                result = false;
            }

            _logger.LogInformation($"Refund Worker Execute End {tpId}");
            return result;
        }
    }
}
