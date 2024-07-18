using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace ThirdPartyWager.Service.BLL;

public class WagerIMBLL
{
    private readonly ApiIMBLL _apiIMBll;
    private readonly WagerIMService _wagerIMService;
    private readonly TpUnsettleService _tpUnsettleService;
    private readonly ThirdPartyBLL _thirdPartyBll;
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly ILogger<WagerIMBLL> _logger;
    private readonly Tp _tp = Tp.IM;
    private const int TpId = (int)Tp.IM;
    private const int TimeZone = 8; // Report/GetBetLog時區為+8
    private const string TimeFormat = "yyyy-MM-dd HH.mm.ss";

    private readonly Dictionary<IMProductWalletEnum, Func<GetBetListByTimeParam, Task<bool>>> _getWagersHandlerMapper;
    private readonly Dictionary<int, IMProductWalletEnum> _gameTypeMapper = new()
    {
        { 1, IMProductWalletEnum.IMSportsbook },
        { 11, IMProductWalletEnum.IMESports }
    };

    private class IMErrorCode
    {
        public const string Success = "0";
        public const string NoData = "558";
    }

    public WagerIMBLL(IServiceProvider provider)
    {
        _apiIMBll = provider.GetRequiredService<ApiIMBLL>();
        _wagerIMService = provider.GetRequiredService<WagerIMService>();
        _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
        _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
        _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        _logger = provider.GetRequiredService<ILogger<WagerIMBLL>>();

        _getWagersHandlerMapper = new Dictionary<IMProductWalletEnum, Func<GetBetListByTimeParam, Task<bool>>>
        {
            {IMProductWalletEnum.IMSportsbook, IMSportsGetWagersHandlerAsync},
            {IMProductWalletEnum.IMESports, IMESportsGetWagersHandlerAsync}
        };
    }

    public async Task<int> ExecWagerWorker(IMConfig config, TpListModel siteData, int type)
    {
        var siteId = siteData.SiteId;
        var quick = false;
        const int runTime = 120; // sec
        const int quickTime = 80; // sec
        const int excess = 120; // min
        const int dataDelayTime = 5; // min，體育類資料更新最大延遲5分
        var timeRange = 10; // min，最大可拉24HR

        foreach (var (gameType, productWallet) in _gameTypeMapper)
        {
            var workerTime = GetWorkerTime(type, dataDelayTime * -60, siteId, gameType);

            quick = CheckQuick(workerTime, excess);

            if (quick)
                timeRange = excess;

            var time = _dateTimeHelper.GetTimeByMinute(timeRange - 1, TimeZone, TimeFormat, workerTime);
            var cvTime = DateTimeOffset.Now
                .ToOffset(new TimeSpan(TimeZone, 0, 0))
                .ToUnixTimeSeconds();

            if (time.startUnix >= cvTime)
                continue;

            var result = await GetWagersAsync(time, siteData, productWallet, config);
            if (result)
                _thirdPartyBll.SetThirdPartyTimerByReference(TpId, type, time.endUnix, gameType, siteId.ToString());
        }

        return quick ? quickTime : runTime;
    }

    #region Private

    private async Task<bool> GetWagersAsync(TimeCast wagerTime, TpListModel tpInfo, IMProductWalletEnum productWallet, IMConfig config)
    {
        const string workerName = "IM_GetWagers";
        string msg;
        try
        {
            var param = new GetBetListByTimeParam
            {
                Setting = config,
                ProductWallet = productWallet,
                StartTime = wagerTime.startTime,
                EndTime = wagerTime.endTime,
            };

            var success = await _getWagersHandlerMapper[productWallet].Invoke(param);
            if (!success)
                return false;

            msg = "FINISH";
            _logger.LogInformation($"{workerName} {msg}\r\n, ProductWallet={productWallet}\r\n{wagerTime.startTime}~{wagerTime.endTime}");
            return true;
        }
        catch (Exception ex)
        {
            msg = "FATAL_ERROR";
            _logger.LogError(ex, $"{workerName} {msg}\r\n, ProductWallet={productWallet}\r\n{ex.Message}");
            return false;
        }
    }

    private async Task<bool> IMSportsGetWagersHandlerAsync(GetBetListByTimeParam param)
    {
        var workerName = param.ProductWallet + "_GetWagers";
        var page = 1;
        var totalPage = 1;
        while (page <= totalPage)
        {
            param.Page = page;
            param.DateFilterType = 4; // IM Sportsbook 最後更新時間
            param.Product = null;

            var resp = await _apiIMBll.GetSportsBetListByTime<IMSportsbookModel>(param, _tp);

            if (resp.Code == IMErrorCode.NoData)
            {
                _logger.LogInformation($"{workerName} NO_DATA");
                page++;
                continue;
            }

            if (resp.Code != IMErrorCode.Success)
            {
                _logger.LogError($"{workerName} THIRD_PARTY_ERROR: Code={resp.Code}, Message={resp.Message}");
                return false;
            }


            totalPage = resp.Pagination.TotalPage;

            try
            {
                var wagers = resp.Result.Select(x => new IMSportsWager(x));
                var result = _wagerIMService.InsertSportsWager(wagers);
                var count = resp.Result.Count();
                if (result != count)
                {
                    _logger.LogInformation(
                        $"{workerName} INSERT_WAGER COUNT_NOT_MATCH: affected={result}, data count={count}");
                }

                BatchInsertTpUnsettle(new BatchTpUnsettleModel
                {
                    Tp = _tp,
                    GameType = GameType.IM_Sports,
                    Infos = wagers
                        .Where(x => x.IsSettled == "1")
                        .Select(x => new TpUnsettleInfo { TransId = x.BetId })
                }, workerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{workerName} INSERT_WAGER FAILED: {ex.Message}");
                return false;
            }

            page++;
        }

        return true;
    }


    // 1: 電競 eSportsBulls, 2: ESports Virtual, 3: PK10
    private readonly Dictionary<string, int> _eSportsProduct = new()
    {
        { "eSportsBulls", 1 },
        { "ESportsVirtual", 2 }
    };

    private async Task<bool> IMESportsGetWagersHandlerAsync(GetBetListByTimeParam param)
    {
        foreach (var (productName, product) in _eSportsProduct)
        {
            var workerName = $"{param.ProductWallet}_{productName}_GetWagers";
            var page = 1;
            var totalPage = 1;
            while (page <= totalPage)
            {
                param.Page = page;
                param.DateFilterType = 3; // IM ESports 最後更新時間
                param.Product = product;

                var resp = await _apiIMBll.GetSportsBetListByTime<IMESportsModel>(param, _tp);

                if (resp.Code == IMErrorCode.NoData)
                {
                    _logger.LogInformation($"{workerName} NO_DATA");
                    page++;
                    continue;
                }

                if (resp.Code != IMErrorCode.Success)
                {
                    _logger.LogError($"{workerName} THIRD_PARTY_ERROR: Code={resp.Code}, Message={resp.Message}");
                    return false;
                }

                totalPage = resp.Pagination.TotalPage;

                try
                {
                    var wagers = resp.Result.Select(x => new IMESportsWager(x));
                    var result = _wagerIMService.InsertESportsWager(wagers);
                    var count = resp.Result.Count();
                    if (result != count)
                    {
                        _logger.LogInformation(
                            $"{workerName} INSERT_WAGER COUNT_NOT_MATCH: affected={result}, data count={count}");
                    }

                    BatchInsertTpUnsettle(new BatchTpUnsettleModel
                    {
                        Tp = _tp,
                        GameType = GameType.IM_ESports,
                        Infos = wagers
                            .Where(x => x.IsSettled == "1")
                            .Select(x => new TpUnsettleInfo { TransId = x.BetId })
                    }, workerName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{workerName} INSERT_WAGER FAILED: {ex.Message}");
                    return false;
                }

                page++;
            }
        }

        return true;
    }

    private void BatchInsertTpUnsettle(BatchTpUnsettleModel input, string workerName)
    {
        var count = input.Infos.Count();
        var result = _tpUnsettleService.BatchInsertTpUnsettle(input);

        if (result != count)
        {
            _logger.LogInformation(
                $"{workerName} INSERT_UNSETTLE_WAGER COUNT_NOT_MATCH: affected={result}, data count={count}");
        }
    }

    private DateTime GetWorkerTime(int type, int vary, int siteId, int gameType)
    {
        var lastTime =
            _thirdPartyBll.GetThirdPartyTimerReferenceByReference(TpId, type, gameType, siteId.ToString());

        return lastTime == null
            ? DateTimeOffset.UtcNow.DateTime.AddHours(-8)
            : lastTime.StartTime.AddSeconds(vary);
    }

    private static bool CheckQuick(DateTime workTime, int excess)
    {
        var now = DateTimeOffset.UtcNow.DateTime;
        var diff = now - workTime;

        return now > workTime && diff.TotalMinutes > excess;
    }

    #endregion
}