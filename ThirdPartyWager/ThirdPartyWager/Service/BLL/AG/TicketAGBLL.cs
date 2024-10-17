using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class TicketAGBLL : ITpRefundBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiAGBLL _apiAGBLL;
        private readonly WagerAGService _wagerAGService;
        private readonly ILogger<TicketAGBLL> _logger;
        private readonly Dictionary<int, AGConfig> _siteConfigDic;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly IEnumerable<ServerInfo> _serverList;
        private readonly int _tpId = (int)Tp.AG;
        private readonly string _tpName = Tp.AG.ToString();
        private readonly int _timeRange = 10;
        private readonly int _timeZone = 0;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        private readonly int _delay = 3600;
        private readonly int _delayTimeRange = 60;

        public TicketAGBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiAGBLL = provider.GetRequiredService<ApiAGBLL>();
            _wagerAGService = provider.GetRequiredService<WagerAGService>();
            _logger = provider.GetRequiredService<ILogger<TicketAGBLL>>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<AGConfig>(_tpName);
            _serverList = provider.GetRequiredService<CommonBLL>().GetSiteList();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }

        public IEnumerable<dynamic> GetPendingList(int gameType, TimeCast ticketTime)
        {
            return _wagerAGService.GetPendingList(ticketTime);
        }

        public async Task<bool> CheckTicketStatus(dynamic pending, int gameType)
        {
            LogContext.PushProperty("SystemJob", $"Auto-Dispatch");
            var func = "CheckTicketStatusAsync";
            var task = true;
            string msg;

            // use TpUserName get TpCurrency and SiteId
            TpCustInfo tpCustInfo = _thirdPartyBLL.GetTpCustInfoWithCache(_tpId, pending.playname);
            if (tpCustInfo == null)
            {
                task = false;
                msg = $"{func} TpCustInfo_ERROR (gameType={gameType}, transactionID={pending.transactionID})";
                _logger.LogError($"{msg}");
                return task;
            }

            var config = _siteConfigDic[tpCustInfo.SiteId];
            var siteName = _serverList.FirstOrDefault(i => i.SiteId == tpCustInfo.SiteId).SiteName;

            var data = await _apiAGBLL.CheckTicketStatus(config.CommonUrl, pending.transactionID);

            if (data == null)
            {
                msg = $"{func} NO_DATA ({gameType})";
                _logger.LogInformation($"{msg}");
                return task;
            }
            var type = data.result.row[0].status;
            if (type == "3")
            {
                // call sw api
                try
                {
                    var response = await _apiAGBLL.callSeamlessApi("Refund", pending);
                    var result = JsonConvert.DeserializeObject<SeamlessAgResponse>(response);
                    if (result.TransferResponse.ResponseCode != "OK")
                    {
                        task = false;
                        msg = $"{func} SEAMLESS_ERROR (gameType={gameType}, type={type}, transactionID={pending.transactionID})";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(result)}");
                        return task;
                    }
                    else
                    {
                        _logger.LogInformation($"System Refund (gameType={gameType}, type={type}, transactionID={pending.transactionID})");
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
                _logger.LogError($"Refund Not Match ({gameType}): {pending.transactionID} {JsonConvert.SerializeObject(data)}");
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
