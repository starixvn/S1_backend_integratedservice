using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL;

public class WagerAMBBLL
{
    private readonly ILogger<WagerAMBBLL> _logger;
    private readonly int _tpId = (int)Tp.AMB;
    private readonly string _tpName = Tp.AMB.ToString();
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly ApiAMBBLL _apiAMBBLL;
    private readonly WagerAMBService _wagerAMBService;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly CommonBLL _commonBLL;
    private readonly TpUnsettleService _tpUnsettleService;
    private readonly IEnumerable<TpCurrency> _tpCurrencyList;
    private readonly int _timeZone = 8;
    private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
    private readonly Dictionary<int, AMBConfig> _siteConfigDic;

    public WagerAMBBLL(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<WagerAMBBLL>>();
        _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        _apiAMBBLL = provider.GetRequiredService<ApiAMBBLL>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _commonBLL = provider.GetRequiredService<CommonBLL>();
        _wagerAMBService = provider.GetRequiredService<WagerAMBService>();
        _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
        _tpCurrencyList = provider.GetRequiredService<CommonBLL>().GetTpCurrencyList(_tpId);
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<AMBConfig>(_tpName);
    }

    public async Task<bool> ExecWagerWorker(int type, int timeRange)
    {
        var wokerTime = GetWorkerTime(type, -120);
        var quick = CheckQuick(wokerTime, 3);
        TimeCast time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, wokerTime);

        var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
        if (time.endUnix < cvTime)
        {
            var result = await GetWagerAsync(time.startTime, time.endTime);
            if (result)
            {
                _thirdPartyBLL.SetThirdPartyTimer(_tpId, type, time.endUnix);
            }
        }

        return quick;
    }

    public async Task<bool> ExecWagerRedoWorker(int type, int timeRange)
    {
        var wokerTime = GetWorkerTime(type, 1);
        var quick = CheckQuick(wokerTime, 60);
        TimeCast time = _dateTimeHelper.GetTimeByMinute(timeRange, _timeZone, _timeFormat, wokerTime);

        var cvTime = DateTimeOffset.Now.AddHours(-1).ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
        if (time.endUnix < cvTime)
        {
            var result = await GetWagerAsync(time.startTime, time.endTime, true);
            if (result)
            {
                _thirdPartyBLL.SetThirdPartyTimer(_tpId, type, time.endUnix);
            }
        }

        return quick;
    }

    public async Task<dynamic> GetWagerAsync(string startTime, string endTime, bool redo = false)
    {
        var workerName = redo ? "GetRedoWagersAsync" : "GetWagersAsync";

        var msg = "FATAL_ERROR";
        var ambGameTypeList = new List<int>
        {
            (int)GameType.AMB_Poker,
            (int)GameType.AMB_RNG
        };

        var siteList = _thirdPartyBLL.GetTpListByTpId(_tpId).Where(x => x.TicketKey == 1);
        var serverList = _commonBLL.GetSiteList();

        foreach (var site in siteList)
        {
            var _setting = _siteConfigDic[site.SiteId];
            if (_setting == null) continue;

            foreach (var gameType in ambGameTypeList)
            {
                var currencyList = JsonConvert.DeserializeObject<IEnumerable<int>>(serverList.First(x => x.SiteId == site.SiteId).CurrencyList);
                foreach (var item in _tpCurrencyList.Where(x => currencyList.Contains(x.CurrencyId)))
                {
                    var currency = item.TpCurrencyId;
                    try
                    {
                        if (gameType == (int)GameType.AMB_Poker && !_setting.PokerCurrencyData.ContainsKey(item.TpCurrencyId))
                        {
                            continue;
                        }

                        if (gameType == (int)GameType.AMB_RNG && !_setting.SlotCurrencyData.ContainsKey(item.TpCurrencyId))
                        {
                            continue;
                        }

                        Thread.Sleep(5000);
                        var getWager = await _apiAMBBLL.GetBetListByTime(startTime, endTime, currency, gameType, site.SiteId);
                        if (getWager.status.code != 0)
                        {
                            msg = "THIRD_PARTY_ERROR";
                            _logger.LogError($"{workerName} {msg}\r\nGameType:{(GameType)gameType}, SiteId:{site.SiteId}, Currency:{currency}\r\n{JsonConvert.SerializeObject(getWager)}");
                            return false;
                        }

                        if (getWager.data.totalRecords == 0 || getWager.data.result == null)
                        {
                            msg = "NO_DATA";
                            _logger.LogInformation($"{workerName} {msg}\r\nGameType:{(GameType)gameType}, SiteId:{site.SiteId}, Currency:{currency}\r\n{JsonConvert.SerializeObject(getWager)}");
                            continue;
                        }

                        var allBetRecords = getWager.data.result
                            .SelectMany(user => user.result)
                            .ToList();

                        foreach (var wager in allBetRecords)
                        {
                            var result = InsertNewAMBWager(wager, gameType);
                        }
                    }
                    catch (Exception ex)
                    {
                        msg = "FATAL_ERROR";
                        _logger.LogError(ex, $"{workerName} {msg}\r\nGameType={gameType}, SiteId={site.SiteId}, Currency={currency}\r\n{ex.Message}");
                        return false;
                    }
                }
            }
        }

        msg = "FINISH";
        _logger.LogInformation($"{workerName} {msg}\r\n{startTime} ~ {endTime}");
        return true;
    }

    private int InsertNewAMBWager(betRecord_AMB wagerData, int gameType)
    {
        var result = _wagerAMBService.InsertAMBWager(wagerData, gameType);
        if (result == 1)
        {
            _ = gameType switch
            {
                (int)GameType.AMB_Poker => _tpUnsettleService.InsertTpUnsettle(Tp.AMB, (GameType)gameType,
                    $"{wagerData.roundId}_{wagerData.username}"),
                (int)GameType.AMB_RNG => _tpUnsettleService.InsertTpUnsettle(Tp.AMB, (GameType)gameType,
                    wagerData.roundId),
                _ => throw new ArgumentOutOfRangeException(nameof(gameType), gameType, null)
            };
        }

        return result;
    }

    private DateTime GetWorkerTime(int type, int vary)
    {
        var lastTime = _thirdPartyBLL.GetThirdPartyTimer(_tpId, type);
        return (lastTime == null) ? DateTime.Now.AddHours(-1) : lastTime.StartTime.AddSeconds(vary);
    }

    private static bool CheckQuick(DateTime workTime, int excess)
    {
        var diff = DateTime.Now - workTime;
        return DateTime.Now > workTime && diff.TotalMinutes > excess;
    }
}