using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL;

public class WagerSPRIBEBLL
{
    private readonly ApiIMBLL _apiIMBll;
    private readonly WagerIMService _wagerIMService;
    private readonly CommonService _commonService;
    private readonly ThirdPartyBLL _thirdPartyBll;
    private readonly DateTimeHelper _dateTimeHelper;
    private readonly ILogger<WagerSPRIBEBLL> _logger;
    private readonly Tp _tp = Tp.SPRIBE;
    private const int TpId = (int)Tp.SPRIBE;
    private const int TimeZone = 8; // Report/GetBetLog時區為+8
    private const string TimeFormat = "yyyy-MM-dd HH.mm.ss";

    private class SPRIBEErrorCode
    {
        public const string Success = "0";
        public const string NoData = "558";
    }

    public WagerSPRIBEBLL(IServiceProvider provider)
    {
        _apiIMBll = provider.GetRequiredService<ApiIMBLL>();
        _wagerIMService = provider.GetRequiredService<WagerIMService>();
        _commonService = provider.GetRequiredService<CommonService>();
        _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
        _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        _logger = provider.GetRequiredService<ILogger<WagerSPRIBEBLL>>();
    }

    public async Task<int> ExecWagerWorker(SPRIBEConfig config, TpListModel siteData, int type)
    {
        var siteId = siteData.SiteId;
        const int runTime = 120; // sec
        const int excess = 120; // min
        const int dataDelayTime = 15; // SPRIBE GetBetLog 資料更新最大延遲15分
        const int timeRange = 10; // min，SPRIBE GetBetLog 時間區間最大10分鐘，資料更新最大延遲15分
        const int gameType = 2;

        var workerTime = GetWorkerTime(type, runTime * -1, siteId, gameType);
        var quick = CheckQuick(workerTime, excess);

        var time = _dateTimeHelper.GetTimeByMinute(timeRange - 1, TimeZone, TimeFormat, workerTime);
        var cvTime = DateTimeOffset.Now
            .ToOffset(new TimeSpan(TimeZone, 0, 0))
            .AddMinutes(dataDelayTime * -1)
            .ToUnixTimeSeconds();

        if (time.endUnix >= cvTime)
            return runTime;

        var result = await GetWagersAsync(time, siteData, IMProductWalletEnum.IMSlot, config);
        if (result)
            _thirdPartyBll.SetThirdPartyTimerByReference(TpId, type, time.endUnix, gameType, siteId.ToString());


        return quick ? GetQuickSec() : runTime;
    }

    #region Private

    private async Task<bool> GetWagersAsync(TimeCast wagerTime, TpListModel tpInfo, IMProductWalletEnum productWallet, SPRIBEConfig config)
    {
        const string workerName = "SPRIBE_GetWagers";
        string msg;
        try
        {
            var param = new GetBetListByTimeParam
            {
                Setting = config,
                ProductWallet = productWallet,
                StartTime = wagerTime.startTime,
                EndTime = wagerTime.endTime
            };

            var success = await SPRIBEGetWagersAsync(param);
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

    private const int DelaySec = 8;
    private async Task<bool> SPRIBEGetWagersAsync(GetBetListByTimeParam param)
    {
        var workerName = param.ProductWallet + "_GetWagers";
        var tpCurrencyList = _commonService.GetTpCurrencyList(TpId)
            .Select(x => x.TpCurrencyId)
            .Distinct();

        foreach (var tpCurrency in tpCurrencyList)
        {
            param.Currency = tpCurrency;

            var page = 1;
            var totalPage = 1;
            while (page <= totalPage)
            {
                param.Page = page;
                var resp = await _apiIMBll.GetBetListByTime<IMSlotModel>(param, _tp);

                if (resp.Code == SPRIBEErrorCode.NoData)
                {
                    _logger.LogInformation($"{workerName} {tpCurrency} NO_DATA");
                    await Task.Delay(DelaySec * 1000); // 1分鐘內發送多筆會被收到併發錯誤，稍停
                    page++;
                    continue;
                }

                if (resp.Code != SPRIBEErrorCode.Success)
                {
                    _logger.LogError($"{workerName} THIRD_PARTY_ERROR: Code={resp.Code}, Message={resp.Message}");
                    return false;
                }

                totalPage = resp.Pagination.TotalPage;

                try
                {
                    var wagers = resp.Result.Select(x => new IMSlotWager(x));
                    var result = _wagerIMService.InsertSlotWager(wagers);
                    var count = resp.Result.Count();
                    if (result != count)
                    {
                        _logger.LogInformation(
                            $"{workerName} INSERT_WAGER COUNT_NOT_MATCH: affected={result}, data count={count}");
                    }
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

    private int GetQuickSec()
    {
        var tpCurrencyList = _commonService.GetTpCurrencyList(TpId);
        return DelaySec * (tpCurrencyList.Count() + 1);
    }

    #endregion
}