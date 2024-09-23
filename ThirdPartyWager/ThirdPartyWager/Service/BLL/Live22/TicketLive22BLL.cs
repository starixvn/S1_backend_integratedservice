using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class TicketLive22BLL : ITpRefundBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiMaxGameBLL _apiMaxGameBLL;
        private readonly WagerLive22Service _wagerLive22Service;
        private readonly ILogger<TicketLive22BLL> _logger;
        // 共用maxgameconfig model
        private readonly Dictionary<int, MaxGameConfig> _siteConfigDic;
        private readonly int _tpId = (int)Tp.Live22;
        private readonly string _tpName = Tp.Live22.ToString();
        private readonly IEnumerable<ServerInfo> _serverList;

        public TicketLive22BLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiMaxGameBLL = provider.GetRequiredService<ApiMaxGameBLL>();
            _wagerLive22Service = provider.GetRequiredService<WagerLive22Service>();
            _logger = provider.GetRequiredService<ILogger<TicketLive22BLL>>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MaxGameConfig>(_tpName);
            _serverList = provider.GetRequiredService<CommonBLL>().GetSiteList();
        }

        public IEnumerable<dynamic> GetPendingList(int gameType)
        {
            return _wagerLive22Service.GetPendingList();
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
                        _logger.LogInformation($"System Refund (gameType={gameType}, type={type}, TicketNo={bettrans.TicketNo}");
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
