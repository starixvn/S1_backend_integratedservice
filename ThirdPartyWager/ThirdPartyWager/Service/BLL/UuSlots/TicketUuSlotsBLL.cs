using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class TicketUuSlotsBLL : ITpRefundBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiMaxGameBLL _apiMaxGameBLL;
        private readonly WagerUuSlotsService _wagerUuSlotsService;
        private readonly ILogger<TicketUuSlotsBLL> _logger;
        // 共用maxgameconfig model
        private readonly Dictionary<int, MaxGameConfig> _siteConfigDic;
        private readonly int _tpId = (int)Tp.UuSlots;
        private readonly string _tpName = Tp.UuSlots.ToString();
        private readonly IEnumerable<ServerInfo> _serverList;

        public TicketUuSlotsBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiMaxGameBLL = provider.GetRequiredService<ApiMaxGameBLL>();
            _wagerUuSlotsService = provider.GetRequiredService<WagerUuSlotsService>();
            _logger = provider.GetRequiredService<ILogger<TicketUuSlotsBLL>>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MaxGameConfig>(_tpName);
            _serverList = provider.GetRequiredService<CommonBLL>().GetSiteList();
        }

        public IEnumerable<dynamic> GetPendingList(int gameType)
        {
            return _wagerUuSlotsService.GetPendingList();
        }

        public async Task<bool> CheckTicketStatus(dynamic bettrans, int gameType)
        {
            LogContext.PushProperty("SystemJob", $"Auto-Dispatch");
            var func = "CheckTicketStatusAsync";
            var task = true;

            // use TpUserName get TpCurrency and SiteId
            TpCustInfo tpCustInfo = _thirdPartyBLL.GetTpCustInfoWithCache(_tpId, bettrans.OperatorCustId);
            var config = _siteConfigDic[tpCustInfo.SiteId];
            var siteName = _serverList.FirstOrDefault(i => i.SiteId == tpCustInfo.SiteId).SiteName;

            var param = new CheckTicketStatusPayloadMaxGame
            {
                OperatorCustId = bettrans.OperatorCustId,
                TicketNo = bettrans.TicketNo,
                AgentKey = tpCustInfo.TpCurrencyId,
            };

            TicketDataMaxGame data = await _apiMaxGameBLL.CheckTicketStatus(config, _tpName, param);

            string msg;
            if (data == null)
            {
                msg = $"{func} NO_DATA ({gameType})";
                _logger.LogInformation($"{msg}");
                return task;
            }

            if (data.ErrorCode == 0)
            {
                // call sw api
                try
                {
                    var type = data.Payload.Status;
                    var response = _apiMaxGameBLL.CallSeamlessApi(config, _tpName, siteName, bettrans, data).Result;
                    if (response == null)
                    {
                        task = false;
                        msg = $"{func} SEAMLESS_NULL (gameType={gameType}, type={type}, TicketNo={bettrans.TicketNo})";
                        _logger.LogError($"{msg}");
                        return task;
                    }

                    var result = JsonConvert.DeserializeObject<SeamlessMaxGameResponse>(response);
                    if (result.ErrorCode != 0)
                    {
                        task = false;
                        msg = $"{func} SEAMLESS_ERROR (gameType={gameType}, type={type}, TicketNo={bettrans.TicketNo})";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(result)}");
                        return task;
                    }
                    else
                    {
                        _logger.LogInformation($"System Refund (gameType={gameType}, type={type}, TicketNo={bettrans.TicketNo})");
                    }
                }
                catch (Exception ex)
                {
                    task = false;
                    _logger.LogError(ex, ex.Message);
                }
            }
            else
            {
                task = false;
                _logger.LogError($"Refund Not Match ({gameType}): {bettrans.TicketNo} {JsonConvert.SerializeObject(data)}");
            }
            return task;
        }
    }
}
