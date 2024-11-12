using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class TicketOne789BLL : ITpRefundBLL
    {
        private readonly ApiOne789BLL _apiOne789BLL;
        private readonly WagerOne789Service _wagerOne789Service;
        private readonly ILogger<TicketOne789BLL> _logger;
        private readonly One789Config _siteConfig;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly int _timeRange = 10;
        private readonly int _timeZone = 0;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        private readonly int _delay = 3600;
        private readonly int _delayTimeRange = 60;

        public TicketOne789BLL(IServiceProvider provider)
        {
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _apiOne789BLL = provider.GetRequiredService<ApiOne789BLL>();
            _wagerOne789Service = provider.GetRequiredService<WagerOne789Service>();
            _logger = provider.GetRequiredService<ILogger<TicketOne789BLL>>();
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<One789Config>(Tp.One789.ToString());
        }

        public IEnumerable<dynamic> GetPendingList(int gameType, TimeCast ticketTime)
        {
            return _wagerOne789Service.GetPendingList(ticketTime, gameType);
        }

        public async Task<bool> CheckTicketStatus(dynamic pending, int gameType)
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

            var data = await _apiOne789BLL.CheckTicketStatus(_siteConfig, param);
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
                    var response = _apiOne789BLL.CallSeamlessApi("Refund", pending).Result;
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
