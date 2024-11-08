using Newtonsoft.Json;

using Serilog.Context;

using ThirdPartyWager.Extensions;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class WagerTCG : BackgroundService
    {
        private readonly ILogger<WagerTCG> _logger;
        private readonly TCGBLL _tcgBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;
        private readonly IEnumerable<TpCurrency> _tpCurrency;
        private readonly IEnumerable<ServerInfo> _serverInfoList;
        private readonly IEnumerable<TpListModel> _siteList;

        private Timer _timer;
        private Timer _unSettleTimer;
        private Timer _cancelTimer;
        private Timer _redoTimer;

        private readonly int tpId = (int)Tp.TCG;
        private readonly string _tpName = Tp.TCG.ToString();
        private readonly int TimeZone = 8;

        public WagerTCG(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerTCG>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tcgBLL = provider.GetRequiredService<TCGBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
            _tpCurrency = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList(tpId);
            _serverInfoList = _commonBLL.GetSiteList();
            _siteList = _thirdPartyBLL.GetTpListByTpId(tpId);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            _unSettleTimer = new Timer(DoUnsettle, null, TimeSpan.Zero, TimeSpan.Zero);
            _cancelTimer = new Timer(DoCancel, null, TimeSpan.Zero, TimeSpan.Zero);
            _redoTimer = new Timer(DoRedo, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private void DoTask(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            LogContext.PushProperty("WagerTimerType", WagerTimerType.Wager.ToString());
            var quick = 0;
            var failed = 0;
            var intervalMins = 2;
            try
            {               
                foreach (var site in _siteList)
                {
                    if (site.TicketKey == 0)
                    {
                        continue;
                    }
                    
                    var currencyList = JsonConvert.DeserializeObject<IEnumerable<int>>(_serverInfoList.First(x => x.SiteId == site.SiteId).CurrencyList);
                    foreach (var currency in _tpCurrency.Where(x => currencyList.Contains(x.CurrencyId)).Select(i => i.TpCurrencyId))
                    {
                        try
                        {
                            var res = Main(currency, site.SiteId);
                            if (res.Result == 1)
                            {
                                quick = 1;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"WagerTCG.DoSettle.{currency}.Exception");
                            Interlocked.Increment(ref failed);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = (quick == 1) ? TimeSpan.FromSeconds(5) : TimeSpan.FromMinutes(intervalMins);
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private async Task<int> Main(string currency, int siteId)
        {
            var quick = 0;
            var timerType = WagerTimerType.Wager;
            var dataRangeMins = 5;

            var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, (int)timerType, currency);
            var thisTime = lastTime?.StartTime.AddMinutes(dataRangeMins) ?? DateTime.Now.BeginningOfTheDay();

            if (thisTime < DateTime.Now.AddMinutes(-10))
            {
                var batchTime = new DateTimeOffset(thisTime);
                var timezoneBatchTime = batchTime.ToOffset(new TimeSpan(TimeZone, 0, 0));
                var isSuccess = currency == "VNDK" ?
                    await _tcgBLL.GetWagersAsync<TCGVNWagerData>(timezoneBatchTime, currency, timerType, siteId) :
                    await _tcgBLL.GetWagersAsync<TCGSEAWagerData>(timezoneBatchTime, currency, timerType, siteId);
                if (isSuccess)
                {
                    _logger.LogInformation($"TCG {currency} Get {timezoneBatchTime:yyyyMMddHHmm} batch file success.");
                    _thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, (int)timerType, batchTime.ToUnixTimeSeconds(), currency);
                }
            }

            if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > dataRangeMins)
            {
                quick = 1;
            }
            return quick;
        }

        private void DoUnsettle(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            LogContext.PushProperty("WagerTimerType", WagerTimerType.UnSettle.ToString());

            var quick = 0;
            var failed = 0;
            var intervalMins = 5;
            try
            {
                foreach (var site in _siteList)
                {
                    if (site.TicketKey == 0)
                    {
                        continue;
                    }

                    var currencyList = JsonConvert.DeserializeObject<IEnumerable<int>>(_serverInfoList.First(x => x.SiteId == site.SiteId).CurrencyList);
                    foreach (var currency in _tpCurrency.Where(x => currencyList.Contains(x.CurrencyId)).Select(i => i.TpCurrencyId))
                    {
                        try
                        {
                            var res = MainUnsettle(currency, site.SiteId);
                            if (res.Result == 1)
                            {
                                quick = 1;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"WagerTCG.DoUnsettle.{currency}.Exception");
                            Interlocked.Increment(ref failed);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = (quick == 1) ? TimeSpan.FromSeconds(5) : TimeSpan.FromMinutes(intervalMins);
                _unSettleTimer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private async Task<int> MainUnsettle(string currency, int siteId)
        {
            var quick = 0;
            var timerType = (int)WagerTimerType.UnSettle;
            var dataRangeMins = 5;

            var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, timerType, currency);
            var thisTime = lastTime?.StartTime.AddMinutes(dataRangeMins) ?? DateTime.Now.BeginningOfTheDay();
            if (thisTime > DateTime.Now)
            {
                return quick;
            }

            var batchTime = new DateTimeOffset(thisTime);
            var timezoneBatchTime = batchTime.ToOffset(new TimeSpan(TimeZone, 0, 0));

            bool isSuccess;
            if (currency == "VNDK" && batchTime.Is15Minute())
            {
                //15分鐘一個批次
                isSuccess = await _tcgBLL.GetUnsettleAsync<TCGVNWagerData>(timezoneBatchTime, currency, siteId);
                if (!isSuccess)
                {
                    return quick;
                }
            }

            //其餘類型5分鐘一個批次
            isSuccess = await _tcgBLL.GetUnsettleAsync<TCGSEAWagerData>(timezoneBatchTime, currency, siteId);
            if (isSuccess)
            {
                _logger.LogInformation($"TCG {currency} Get Unsettle {timezoneBatchTime:yyyyMMddHHmm} batch file success.");
                _thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, timerType, batchTime.ToUnixTimeSeconds(), currency);
            }

            if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > dataRangeMins)
            {
                quick = 1;
            }
            return quick;
        }

        private async void DoRedo(object state)
        {
            var timerType = WagerTimerType.RedoWager;
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            LogContext.PushProperty("WagerTimerType", timerType.ToString());

            var intervalMins = 10;
            try
            {
                foreach (var redo in _commonBLL.GetTpWagerRedo(tpId))
                {
                    var time = redo.Time;
                    var redoObj = JsonConvert.DeserializeObject<TCGRedoObj>(redo.Parameter);
                    var currency = redoObj.TpCurrencyId;
                    var isSuccess = currency == "VNDK" ?
                         await _tcgBLL.GetWagersAsync<TCGVNWagerData>(time, currency, timerType, redoObj.SiteId) :
                         await _tcgBLL.GetWagersAsync<TCGSEAWagerData>(time, currency, timerType, redoObj.SiteId);

                    if (isSuccess)
                    {
                        _logger.LogInformation($"TCG {currency} Redo Get {time:yyyyMMddHHmm} batch file success.");
                        _commonBLL.DeleteTpWagerRedo(redo);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WagerTCG.{timerType}.Exception");
            }
            finally
            {
                _redoTimer.Change(TimeSpan.FromMinutes(intervalMins), TimeSpan.Zero);
            }
        }

        private void DoCancel(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            LogContext.PushProperty("WagerTimerType", WagerTimerType.UnSettle.ToString());

            var quick = 0;
            var failed = 0;
            var intervalMins = 5;

            try
            {           
                foreach (var site in _siteList)
                {
                    if (site.TicketKey == 0)
                    {
                        continue;
                    }

                    var currency = _tpCurrency.FirstOrDefault(x => x.TpCurrencyId == "VNDK")?.TpCurrencyId;                  
                    try
                    {
                        var res = MainCancel(currency, site.SiteId);
                        if (res.Result == 1)
                        {
                            quick = 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"WagerTCG.DoCancel.{currency}.Exception");
                        Interlocked.Increment(ref failed);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = (quick == 1) ? TimeSpan.FromSeconds(5) : TimeSpan.FromMinutes(intervalMins);
                _cancelTimer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        
        private async Task<int> MainCancel(string currency, int siteId)
        {
            var quick = 0;
            var timerType = WagerTimerType.Cancel;
            var dataRangeMins = 5;

            var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, (int)timerType, currency);
            var thisTime = lastTime?.StartTime.AddMinutes(dataRangeMins) ?? DateTime.Now.BeginningOfTheDay();

            if (thisTime < DateTime.Now.AddMinutes(-10))
            {
                var batchTime = new DateTimeOffset(thisTime);
                var timezoneBatchTime = batchTime.ToOffset(new TimeSpan(TimeZone, 0, 0));
                var isSuccess = await _tcgBLL.GetWagersAsync<TCGVNCancelWagerData>(timezoneBatchTime, currency, timerType, siteId);
                if (isSuccess)
                {
                    _logger.LogInformation($"TCG {currency} Get Cancel {timezoneBatchTime:yyyyMMddHHmm} batch file success.");
                    _thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, (int)timerType, batchTime.ToUnixTimeSeconds(), currency);
                }
            }

            if ((int)DateTime.Now.Subtract(thisTime).TotalMinutes > dataRangeMins)
            {
                quick = 1;
            }
            return quick;
        }
        
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            _unSettleTimer?.Change(Timeout.Infinite, 0);
            _cancelTimer?.Change(Timeout.Infinite, 0);
            _redoTimer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}