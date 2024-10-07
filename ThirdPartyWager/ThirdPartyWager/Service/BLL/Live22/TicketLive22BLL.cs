using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
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
        private readonly Dictionary<int, MaxGameConfig> _siteConfigDic;// 共用maxgameconfig model
        private readonly int _tpId = (int)Tp.Live22;
        private readonly string _tpName = Tp.Live22.ToString();
        private readonly IEnumerable<ServerInfo> _serverList;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeRange = 10;
        private readonly int _timeZone = 0;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        private readonly int _delay = 3600;
        private readonly int _delayTimeRange = 60;

        public TicketLive22BLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiMaxGameBLL = provider.GetRequiredService<ApiMaxGameBLL>();
            _wagerLive22Service = provider.GetRequiredService<WagerLive22Service>();
            _logger = provider.GetRequiredService<ILogger<TicketLive22BLL>>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MaxGameConfig>(_tpName);
            _serverList = provider.GetRequiredService<CommonBLL>().GetSiteList();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        public IEnumerable<dynamic> GetPendingList(int gameType, TimeCast ticketTime)
        {
            return _wagerLive22Service.GetPendingList(ticketTime);
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
                    var response = await _apiMaxGameBLL.CallSeamlessApi(config, _tpName, siteName, bettrans, data);
                    if (response == null)
                    {
                        task = true;
                        msg = $"{func} SEAMLESS_NULL (gameType={gameType}, type={type}, TicketNo={bettrans.TicketNo})";
                        _logger.LogError($"{msg}");
                        return task;
                    }

                    var result = JsonConvert.DeserializeObject<SeamlessMaxGameResponse>(response);
                    if (result.ErrorCode != 0 && result.ErrorCode != 1009)
                    {
                        // 排除1009重複req
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

        public TimeCast GetTicketTime(DateTime startTime, int lockPeriod, out bool exec)
        {
            exec = true;
            var ticketTime = _dateTimeHelper.GetTimeByMinute(_timeRange, _timeZone, _timeFormat, startTime);
            var cvTime = DateTimeOffset.UtcNow.AddMinutes(lockPeriod).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
            if (ticketTime.endUnix > cvTime)
            {
                exec = false;
            }
            else
            {
                var diff = cvTime - ticketTime.endUnix;
                if (diff >= _delay)
                {
                    ticketTime = _dateTimeHelper.GetTimeByMinute(_delayTimeRange, _timeZone, _timeFormat, startTime);
                }
            }
            return ticketTime;
        }
    }
}
