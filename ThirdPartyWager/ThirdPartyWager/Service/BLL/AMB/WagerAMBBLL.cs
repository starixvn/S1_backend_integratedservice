using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL;

public class WagerAMBBLL
{
    private readonly int _tpId = (int)Tp.AMB;
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly ApiAMBBLL _apiAMBBLL;
    private readonly WagerAMBService _wagerAMBService;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly ILogger<WagerAMBBLL> _logger;
    private readonly CommonBLL _commonBLL;
    private readonly TpUnsettleService _tpUnsettleService;
    private readonly IEnumerable<TpCurrency> _tpCurrencyList;
    private readonly int _timeZone = 8;
    private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";

    public WagerAMBBLL(IServiceProvider provider)
    {
        _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        _apiAMBBLL = provider.GetRequiredService<ApiAMBBLL>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _commonBLL = provider.GetRequiredService<CommonBLL>();
        _wagerAMBService = provider.GetRequiredService<WagerAMBService>();
        _logger = provider.GetRequiredService<ILogger<WagerAMBBLL>>();
        _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
        _tpCurrencyList = _commonBLL.GetTpCurrencyList(_tpId);
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
        var quick = CheckQuick(wokerTime, 120);
        var time = _dateTimeHelper.GetTimeByHour(timeRange, _timeZone, _timeFormat, wokerTime);

        var cvTime = DateTimeOffset.Now.ToOffset(new TimeSpan(_timeZone, 0, 0)).ToUnixTimeSeconds();
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

        foreach (var gameType in ambGameTypeList)
        {
            foreach (var item in _tpCurrencyList)
            {
                var currency = item.TpCurrencyId;
                try
                {
                    if (gameType == (int)GameType.AMB_Poker && (currency == "USD" || currency == "CNY"))
                    {
                        continue;
                    }

                    var getWager = await _apiAMBBLL.GetBetListByTime(startTime, endTime, currency, gameType);
                    // sleep 15 sec
                    Thread.Sleep(15000);
                    if (getWager.status.code != 0)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(getWager)}");
                        return false;
                    }

                    if (getWager.data.totalRecords == 0 || getWager.data.result == null)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"AMB GetWager:{msg}\r\nGameType:{(GameType)gameType},Currency:{currency}\r\n{JsonConvert.SerializeObject(getWager)}");
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
                    _logger.LogError(ex, $"{workerName} {msg}\r\ngameType={gameType}\r\ncurrency={currency}\r\n{ex.Message}");
                    return false;
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

    private bool CheckQuick(DateTime workTime, int excess)
    {
        var diff = DateTime.Now - workTime;
        return DateTime.Now > workTime && diff.TotalMinutes > excess;
    }
}