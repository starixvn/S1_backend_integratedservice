using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Library;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerMegaBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly WagerMegaService _wagerMegaService;
        private readonly ApiMegaBLL _apiMegaBLL;
        private readonly ILogger<WagerMegaBLL> _logger;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly IHostEnvironment _environment;

        private readonly int tpId = (int)Tp.Mega;
        private readonly int timeZone = 8;
        private readonly string timeFormat = "yyyy-MM-dd HH:mm:ss";

        public WagerMegaBLL(IServiceProvider provider, IHostEnvironment environment)
        {
            _logger = provider.GetRequiredService<ILogger<WagerMegaBLL>>();
            _wagerMegaService = provider.GetRequiredService<WagerMegaService>();
            _apiMegaBLL = provider.GetRequiredService<ApiMegaBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
            _environment = environment;
        }

        public async Task<bool> ExecReportPlayerListWorker(int type, string currency, int timeRange)
        {
            var wokerTime = GetWorkerTime(type, currency, 1, -5);
            // 取得下個結帳時間
            var settlementTime = GetSettlementTime(wokerTime).AddDays(1);
            var nextSettlementTime = ((DateTimeOffset)settlementTime).ToUnixTimeSeconds() - 1;
            TimeCast time = _dateTimeHelper.GetTimeByDay(timeRange, timeZone, timeFormat, wokerTime);

            // redo
            var deduct = (type == 2) ? -2 : 0;
            var excess = (type == 2) ? 3 : 1;
            bool quick = CheckQuick(wokerTime, excess);
            var cvTime = DateTimeOffset.Now.AddDays(deduct).ToOffset(new TimeSpan(timeZone, 0, 0)).ToUnixTimeSeconds();

            // 檢查結帳時間
            var pending = cvTime < nextSettlementTime;
            if (!pending)
            {
                bool result = await GetReportPlayerListAsync(time, currency);
                if (result)
                {
                    _thirdPartyBLL.SetThirdPartyTimerByCurrency(tpId, type, nextSettlementTime, currency);
                }
                else
                {
                    quick = true;
                }
            }

            return quick;
        }

        private async Task<bool> GetReportPlayerListAsync(TimeCast time, string currency, bool redo = false)
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();

            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                // get report player
                var dataReportPlayer = await _apiMegaBLL.GetReportPlayerListByTime(setting, currency, time.startTime, time.endTime);

                if (dataReportPlayer.code != null)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataReportPlayer)}");
                    return false;
                }

                if (!dataReportPlayer.result.Any())
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataReportPlayer)}");
                    return true;
                }

                foreach (var reportList in dataReportPlayer.result)
                {
                    try
                    {
                        reportList.TransId = $"{time.startUnix}_{reportList.loginId}";
                        reportList.Currency = currency;
                        reportList.StartUnix = time.startUnix;
                        reportList.EndUnix = time.endUnix;
                        var r = _wagerMegaService.InsertReport(reportList).Result;
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_REPORT(PLAYER: {reportList.loginId} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(reportList)}\r\n{ex.Message}");
                        return false;
                    }
                }

                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{currency} {time.startTime} ~ {time.endTime}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }

        public bool GetWagerAsync()
        {
            bool result = true;
            string guid = Guid.NewGuid().ToString();

            // get api info
            var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

            // get report data from db
            var pendingList = _wagerMegaService.GetReportPlayerUnWagerList();
            if (pendingList == null || !pendingList.Any())
            {
                return true;
            }

            var msg = "";
            var current = DateTimeOffset.Now.ToOffset(new TimeSpan(timeZone, 0, 0));
            // get gamelog url api by each 
            foreach (var report in pendingList)
            {
                // convert timezone
                var endSecond = report.AddSecond + 300;
                var dataTime = new DateTimeOffset(report.StartTime).ToOffset(new TimeSpan(timeZone, 0, 0));
                var start = dataTime.AddSeconds(report.AddSecond).ToString(timeFormat);
                var end = dataTime.AddSeconds(endSecond).ToString(timeFormat);
                var diff = current.ToUnixTimeSeconds() - (report.StartAt + report.AddSecond);

                // lock recent 10 min
                if (diff < 600)
                {
                    continue;
                }
                var gameLogUrl = _apiMegaBLL.GetGameLogUrlByPlayer(setting["ApiUrl"], setting["sn"], setting[$"secretCode_{report.Currency}"], report.loginId, start, end, guid);
                if (gameLogUrl.code != "0")
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(gameLogUrl)}");
                    return false;
                }

                // reform parameter to get gamelog api (it is unofficially way to get gamelog detail data)
                var uri = new Uri(gameLogUrl.result);
                var wagerUrl = $"{uri.Scheme}://{uri.Host}";
                var query = QueryHelpers.ParseQuery(uri.Query);
                var p = query.ToDictionary(k => k.Key, k => k.Value.ToString());

                int perPage = 20;
                int page = 1;
                int totalPage = 1;

                while (page <= totalPage)
                {
                    var dataWager = _apiMegaBLL.GetBetListByTime(wagerUrl, p, guid, page);
                    if (!dataWager.success)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return false;
                    }

                    if (dataWager.total == 0 || dataWager.results.Count == 0)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        _wagerMegaService.SetAddSecond(report.loginId, report.StartAt, endSecond);
                        break;
                    }

                    if (totalPage == 1 && dataWager.total > perPage)
                    {
                        decimal cntPage = dataWager.total / perPage;
                        totalPage = (int)cntPage + 1;
                    }

                    // write wager
                    var succ = true;
                    foreach (var bet in dataWager.results)
                    {
                        try
                        {
                            if (bet.GameID == -1)
                            {
                                continue;
                            }
                            bet.TransId = $"{bet.id}_{bet.CreateUnix}";
                            bet.LoginId = report.loginId;
                            bet.Currency = report.Currency;
                            var insert = _wagerMegaService.InsertWager(bet);
                            var res = insert.Result;
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.TransId} )";
                            _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                            succ = false;
                            result = false;
                        }
                    }

                    // update wager finish
                    if (succ)
                    {
                        _wagerMegaService.SetAddSecond(report.loginId, report.StartAt, endSecond);
                    }

                    msg = "FINISH";
                    _logger.LogInformation($"GetWagersAsync {msg}\r\n{start} ~ {end}");
                    page++;
                }
            }
            return result;
        }

        public bool CheckWagerAsync()
        {
            // get un check report data
            var checkingList = _wagerMegaService.GetReportPlayerUnCheckList();
            if (checkingList == null || !checkingList.Any())
            {
                return true;
            }

            var current = DateTime.UtcNow;

            // foreach get wager summary
            foreach (var report in checkingList)
            {
                long end = report.StartAt + 86400 - 1;
                var summary = _wagerMegaService.GetWagerPlayerSummary(report.loginId, report.StartAt, end);

                // check data match or not
                if (summary.Stake == report.bet && summary.WinLostAmount == report.win)
                {
                    // match update checkflag 1 success
                    var r = _wagerMegaService.SetCheckFlag(report.loginId, report.StartAt, 1);
                }
                else
                {
                    DateTime toDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(report.StartAt);
                    var diff = current - toDateTime;
                    if (diff.TotalDays > 4)
                    {
                        //TpReportMega > 4 day update checkflag 2 fail
                        var r = _wagerMegaService.SetCheckFlag(report.loginId, report.StartAt, 2);
                    }
                    else
                    {
                        // un match update AddSecond
                        var r = _wagerMegaService.SetAddSecond(report.loginId, report.StartAt, 0);
                    }
                }
            }

            return checkingList.Count() == 200;
        }

        private DateTime GetWorkerTime(int type, string currency, int vary, int begin)
        {
            var lastTime = _thirdPartyBLL.GetThirdPartyTimerReferenceByCurrency(tpId, type, currency);
            return (lastTime == null) ? DateTime.Now.AddDays(begin) : lastTime.StartTime.AddSeconds(vary);
        }

        private bool CheckQuick(DateTime workTime, int excess)
        {
            var diff = DateTime.Now - workTime;
            return DateTime.Now > workTime && diff.Days > excess;
        }

        private DateTime GetSettlementTime(DateTime time)
        {
            var c = new DateTimeOffset(new DateTime(time.Year, time.Month, time.Day, 14, 0, 0), new TimeSpan(8, 0, 0));
            return c.DateTime;
        }
    }
}
